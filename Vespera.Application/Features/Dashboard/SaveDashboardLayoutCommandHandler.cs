using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard;

public sealed class SaveDashboardLayoutCommandHandler : IRequestHandler<SaveDashboardLayoutCommand, Result>
{
    private readonly IReadRepository<DashboardLayout> _layouts;
    private readonly IWriteRepository<DashboardLayout> _layoutWriter;
    private readonly IEnumerable<IDashboardWidgetProvider> _providers;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public SaveDashboardLayoutCommandHandler(
        IReadRepository<DashboardLayout> layouts, IWriteRepository<DashboardLayout> layoutWriter,
        IEnumerable<IDashboardWidgetProvider> providers, ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _layouts = layouts;
        _layoutWriter = layoutWriter;
        _providers = providers;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SaveDashboardLayoutCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure(Error.Unauthorized("dashboard_layout.not_authenticated", "Not authenticated."));
        }

        var registeredKeys = _providers.Select(p => p.WidgetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = request.Widgets.Select(w => w.WidgetKey).FirstOrDefault(key => !registeredKeys.Contains(key));
        if (unknown is not null)
        {
            return Result.Failure(Error.Validation("dashboard_layout.unknown_widget", $"'{unknown}' is not a registered widget."));
        }

        var widgets = request.Widgets
            .Select(w => (w.WidgetKey, w.SortOrder, w.IsVisible, Enum.Parse<WidgetSize>(w.Size, ignoreCase: true)))
            .ToList();

        var tenantId = _tenantContext.TenantId;
        var userId = new UserId(userIdValue);
        var layout = await _layouts.FirstOrDefaultAsync(new DashboardLayoutByUserSpecification(tenantId, userId), cancellationToken);

        if (layout is null)
        {
            layout = DashboardLayout.CreateDefault(tenantId, userId, widgets);
            await _layoutWriter.AddAsync(layout, cancellationToken);
            return Result.Success();
        }

        var result = layout.ApplyLayout(widgets);
        if (result.IsFailure)
        {
            return result;
        }

        _layoutWriter.Update(layout);
        return Result.Success();
    }
}
