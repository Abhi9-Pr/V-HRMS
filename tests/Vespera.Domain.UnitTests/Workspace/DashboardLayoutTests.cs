using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Workspace;

namespace Vespera.Domain.UnitTests.Workspace;

public class DashboardLayoutTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly UserId UserId = UserId.New();

    [Fact]
    public void CreateDefault_Should_Seed_The_Given_Widgets()
    {
        var layout = DashboardLayout.CreateDefault(TenantId, UserId,
            [("shiftTracker", 0, true, WidgetSize.Medium), ("todos", 1, true, WidgetSize.Small)]);

        layout.Widgets.Should().HaveCount(2);
        layout.Widgets.Should().Contain(w => w.WidgetKey == "shiftTracker" && w.SortOrder == 0);
    }

    [Fact]
    public void ApplyLayout_Should_Reject_Duplicate_Widget_Keys()
    {
        var layout = DashboardLayout.CreateDefault(TenantId, UserId, [("shiftTracker", 0, true, WidgetSize.Medium)]);

        var result = layout.ApplyLayout([("todos", 0, true, WidgetSize.Small), ("todos", 1, true, WidgetSize.Small)]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ApplyLayout_Should_Drop_Widgets_No_Longer_Present()
    {
        var layout = DashboardLayout.CreateDefault(TenantId, UserId,
            [("shiftTracker", 0, true, WidgetSize.Medium), ("todos", 1, true, WidgetSize.Small)]);

        layout.ApplyLayout([("shiftTracker", 0, true, WidgetSize.Large)]);

        layout.Widgets.Should().ContainSingle(w => w.WidgetKey == "shiftTracker");
    }

    [Fact]
    public void ApplyLayout_Should_Update_Existing_Widget_In_Place()
    {
        var layout = DashboardLayout.CreateDefault(TenantId, UserId, [("shiftTracker", 0, true, WidgetSize.Medium)]);

        layout.ApplyLayout([("shiftTracker", 3, false, WidgetSize.Large)]);

        var widget = layout.Widgets.Single();
        widget.SortOrder.Should().Be(3);
        widget.IsVisible.Should().BeFalse();
        widget.Size.Should().Be(WidgetSize.Large);
    }
}
