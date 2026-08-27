using Vespera.Domain.Common;

namespace Vespera.Domain.Workspace;

public readonly record struct WidgetPreferenceId(Guid Value)
{
    public static WidgetPreferenceId New() => new(Guid.NewGuid());
}

/// <summary>One user's saved state for one widget slot on their dashboard. <see cref="WidgetKey"/>
/// is the same string a registered <c>IDashboardWidgetProvider</c> exposes — validated against the
/// live provider registry by <c>SaveDashboardLayoutCommandHandler</c>, not by this type, so a
/// widget that's since been removed from the registry doesn't blow up deserializing an old
/// layout.</summary>
public sealed class WidgetPreference : Entity<WidgetPreferenceId>
{
    internal WidgetPreference(WidgetPreferenceId id, string widgetKey, int sortOrder, bool isVisible, WidgetSize size)
        : base(id)
    {
        WidgetKey = widgetKey;
        SortOrder = sortOrder;
        IsVisible = isVisible;
        Size = size;
    }

    public string WidgetKey { get; }

    public int SortOrder { get; private set; }

    public bool IsVisible { get; private set; }

    public WidgetSize Size { get; private set; }

    internal void Apply(int sortOrder, bool isVisible, WidgetSize size)
    {
        SortOrder = sortOrder;
        IsVisible = isVisible;
        Size = size;
    }
}
