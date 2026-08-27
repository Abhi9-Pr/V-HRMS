using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Domain.Workspace;

public readonly record struct DashboardLayoutId(Guid Value)
{
    public static DashboardLayoutId New() => new(Guid.NewGuid());
}

/// <summary>One row per (tenant, user) — this user's reorder/show-hide/resize state for their
/// landing dashboard. There is no per-widget mutation method by design: the client always owns the
/// full arrangement (drag-drop reorder, a visibility toggle, a size change all produce the same
/// "here is my current layout" snapshot), so <see cref="ApplyLayout"/> replaces the whole set in one
/// call rather than exposing move/hide/resize as three separately-racing operations.</summary>
public sealed class DashboardLayout : AggregateRoot<DashboardLayoutId>, ITenantScoped
{
    private readonly List<WidgetPreference> _widgets = [];

    private DashboardLayout(DashboardLayoutId id, TenantId tenantId, UserId userId)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
    }

    public TenantId TenantId { get; }

    public UserId UserId { get; }

    public IReadOnlyList<WidgetPreference> Widgets => _widgets.AsReadOnly();

    public static DashboardLayout CreateDefault(TenantId tenantId, UserId userId, IReadOnlyList<(string WidgetKey, int SortOrder, bool IsVisible, WidgetSize Size)> defaults)
    {
        var layout = new DashboardLayout(DashboardLayoutId.New(), tenantId, userId);
        layout.ApplyLayout(defaults);
        return layout;
    }

    /// <summary>Replaces the entire widget arrangement. Entries not present in
    /// <paramref name="widgets"/> are dropped (e.g. a widget the caller removed from the
    /// registry); entries present are upserted in place so identity-sensitive concerns (none
    /// today) would survive a re-save.</summary>
    public Result ApplyLayout(IReadOnlyList<(string WidgetKey, int SortOrder, bool IsVisible, WidgetSize Size)> widgets)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var widget in widgets)
        {
            if (string.IsNullOrWhiteSpace(widget.WidgetKey))
            {
                return Result.Failure(Error.Validation("dashboard_layout.widget_key_required", "A widget key is required."));
            }

            if (!keys.Add(widget.WidgetKey))
            {
                return Result.Failure(Error.Validation("dashboard_layout.duplicate_widget", $"Widget '{widget.WidgetKey}' was specified more than once."));
            }
        }

        _widgets.RemoveAll(existing => !keys.Contains(existing.WidgetKey));

        foreach (var widget in widgets)
        {
            var existing = _widgets.FirstOrDefault(w => w.WidgetKey.Equals(widget.WidgetKey, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                existing.Apply(widget.SortOrder, widget.IsVisible, widget.Size);
            }
            else
            {
                _widgets.Add(new WidgetPreference(WidgetPreferenceId.New(), widget.WidgetKey, widget.SortOrder, widget.IsVisible, widget.Size));
            }
        }

        return Result.Success();
    }
}
