using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard;

/// <summary>
/// The landing dashboard's one aggregated read. Every registered <see cref="IDashboardWidgetProvider"/>
/// that the caller currently has visible runs concurrently (<see cref="Task.WhenAll{TResult}"/>),
/// each wrapped in its own try/catch, short cache, and timeout — a widget that throws, times out,
/// or returns <see cref="Result{TValue}.IsFailure"/> becomes one failed envelope in the response,
/// never an exception that blanks the rest of the page. Adding a widget to the dashboard never
/// touches this handler; see <see cref="IDashboardWidgetProvider"/>.
///
/// Each concurrent fetch gets its own DI scope (<see cref="IServiceScopeFactory.CreateScope"/>),
/// not the ambient request scope: most providers reach a DbContext eventually (directly or via
/// <see cref="ISender"/>), and DbContext is neither thread-safe nor reentrant — two widgets sharing
/// the request's one scoped DbContext instance throws "A second operation was started on this
/// context instance before a previous operation completed" the moment their queries overlap. A
/// fresh scope per widget gives each one its own DbContext, the same as if it were its own request.
/// </summary>
public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, Result<DashboardResponseDto>>
{
    private static readonly TimeSpan WidgetCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WidgetTimeout = TimeSpan.FromSeconds(5);

    private static readonly Action<ILogger, string, Guid, Exception?> LogWidgetFailure = LoggerMessage.Define<string, Guid>(
        LogLevel.Error, new EventId(1, nameof(LogWidgetFailure)), "Dashboard widget {WidgetKey} failed to load for tenant {TenantId}");

    private readonly IReadRepository<DashboardLayout> _layouts;
    private readonly IEnumerable<IDashboardWidgetProvider> _providers;
    private readonly IDashboardWidgetCache _cache;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetDashboardQueryHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public GetDashboardQueryHandler(
        IReadRepository<DashboardLayout> layouts, IEnumerable<IDashboardWidgetProvider> providers, IDashboardWidgetCache cache,
        ITenantContext tenantContext, ICurrentUser currentUser, ILogger<GetDashboardQueryHandler> logger, IServiceScopeFactory scopeFactory)
    {
        _layouts = layouts;
        _providers = providers;
        _cache = cache;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<Result<DashboardResponseDto>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        IReadOnlyList<WidgetPreference> saved = [];
        if (_currentUser.UserId is { } userIdValue)
        {
            var layout = await _layouts.FirstOrDefaultAsync(
                new DashboardLayoutByUserSpecification(tenantId, new UserId(userIdValue)), cancellationToken);
            saved = layout?.Widgets ?? [];
        }

        var savedByKey = saved.ToDictionary(w => w.WidgetKey, StringComparer.OrdinalIgnoreCase);

        var layoutDtos = _providers
            .Select(provider => savedByKey.TryGetValue(provider.WidgetKey, out var preference)
                ? new DashboardWidgetPreferenceDto(provider.WidgetKey, preference.SortOrder, preference.IsVisible, preference.Size.ToString())
                : new DashboardWidgetPreferenceDto(provider.WidgetKey, provider.DefaultOrder, provider.DefaultVisible, provider.DefaultSize.ToString()))
            .OrderBy(dto => dto.SortOrder)
            .ToList();

        var orderByKey = layoutDtos.ToDictionary(dto => dto.WidgetKey, dto => dto.SortOrder, StringComparer.OrdinalIgnoreCase);
        var visibleKeys = layoutDtos.Where(dto => dto.IsVisible).Select(dto => dto.WidgetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fetchTasks = _providers
            .Where(provider => visibleKeys.Contains(provider.WidgetKey))
            .Select(provider => FetchWidgetAsync(provider, tenantId.Value, _currentUser.UserId, cancellationToken));

        var envelopes = await Task.WhenAll(fetchTasks);

        var ordered = envelopes.OrderBy(envelope => orderByKey.GetValueOrDefault(envelope.WidgetKey, int.MaxValue)).ToList();

        return Result.Success(new DashboardResponseDto(layoutDtos, ordered));
    }

    private async Task<DashboardWidgetEnvelopeDto> FetchWidgetAsync(
        IDashboardWidgetProvider provider, Guid tenantId, Guid? userId, CancellationToken cancellationToken)
    {
        var size = provider.DefaultSize.ToString();
        var order = provider.DefaultOrder;
        var cacheKey = $"dashboard-widget:{tenantId}:{userId?.ToString() ?? "anon"}:{provider.WidgetKey}";

        if (_cache.TryGet(cacheKey, out var cached))
        {
            return new DashboardWidgetEnvelopeDto(provider.WidgetKey, order, size, true, cached, null);
        }

        using var timeoutCts = new CancellationTokenSource(WidgetTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Resolve a fresh provider instance from its own scope — see the class doc comment on
            // why this can't reuse the ambient request scope's provider (and its DbContext).
            using var scope = _scopeFactory.CreateScope();
            var scopedProvider = scope.ServiceProvider.GetServices<IDashboardWidgetProvider>()
                .Single(p => p.WidgetKey == provider.WidgetKey);

            var result = await scopedProvider.GetPayloadAsync(linkedCts.Token);
            if (result.IsFailure)
            {
                return new DashboardWidgetEnvelopeDto(provider.WidgetKey, order, size, false, null, result.Error.Message);
            }

            _cache.Set(cacheKey, result.Value, WidgetCacheTtl);
            return new DashboardWidgetEnvelopeDto(provider.WidgetKey, order, size, true, result.Value, null);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return new DashboardWidgetEnvelopeDto(provider.WidgetKey, order, size, false, null, "This widget timed out.");
        }
        catch (Exception ex)
        {
            // Never let one widget's bug take down the whole dashboard response — see the class
            // doc comment. The underlying exception is not surfaced to the client (a widget-specific
            // exception isn't actionable to the caller beyond "try again"), but it must still be
            // logged somewhere — LoggingBehavior only sees the overall request as a success, since
            // this handler itself never throws.
            LogWidgetFailure(_logger, provider.WidgetKey, tenantId, ex);
            return new DashboardWidgetEnvelopeDto(provider.WidgetKey, order, size, false, null, "This widget is temporarily unavailable.");
        }
    }
}
