using MediatR;
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
/// </summary>
public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, Result<DashboardResponseDto>>
{
    private static readonly TimeSpan WidgetCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WidgetTimeout = TimeSpan.FromSeconds(5);

    private readonly IReadRepository<DashboardLayout> _layouts;
    private readonly IEnumerable<IDashboardWidgetProvider> _providers;
    private readonly IDashboardWidgetCache _cache;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public GetDashboardQueryHandler(
        IReadRepository<DashboardLayout> layouts, IEnumerable<IDashboardWidgetProvider> providers, IDashboardWidgetCache cache,
        ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _layouts = layouts;
        _providers = providers;
        _cache = cache;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
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
            var result = await provider.GetPayloadAsync(linkedCts.Token);
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
        catch (Exception)
        {
            // Never let one widget's bug take down the whole dashboard response — see the class
            // doc comment. The underlying exception is intentionally not surfaced to the client;
            // LoggingBehavior has already logged the overall request, and a widget-specific
            // exception is not actionable to the caller beyond "try again".
            return new DashboardWidgetEnvelopeDto(provider.WidgetKey, order, size, false, null, "This widget is temporarily unavailable.");
        }
    }
}
