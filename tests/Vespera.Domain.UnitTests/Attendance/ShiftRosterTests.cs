using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Attendance;

public class ShiftRosterTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly ShiftId ShiftId = ShiftId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Default_To_Draft_And_Non_Override()
    {
        var roster = CreateRoster();

        roster.Status.Should().Be(ShiftRosterStatus.Draft);
        roster.IsOverride.Should().BeFalse();
        roster.PublishedAt.Should().BeNull();
        roster.PublishedBy.Should().BeNull();
    }

    [Fact]
    public void Publish_Should_Set_Status_And_Stamp()
    {
        var roster = CreateRoster();

        var result = roster.Publish(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        roster.Status.Should().Be(ShiftRosterStatus.Published);
        roster.PublishedAt.Should().Be(Now);
        roster.PublishedBy.Should().Be("hr@vespera.test");
    }

    [Fact]
    public void Publish_Should_Fail_When_Already_Published()
    {
        var roster = CreateRoster();
        roster.Publish(Now, "hr@vespera.test");

        var result = roster.Publish(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("shift_roster.already_published");
    }

    [Fact]
    public void Reassign_Should_Fail_When_Same_Shift()
    {
        var roster = CreateRoster();

        var result = roster.Reassign(ShiftId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("shift_roster.same_shift");
    }

    private static ShiftRoster CreateRoster(bool isOverride = false)
    {
        var period = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7)).Value;
        return ShiftRoster.Create(TenantId, EmployeeId, ShiftId, period, Now, isOverride);
    }
}
