using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Attendance;

public class RosterAssignmentResolverTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly ShiftId BaseShiftId = ShiftId.New();
    private static readonly ShiftId OverrideShiftId = ShiftId.New();
    private static readonly DateOnly Target = new(2026, 3, 10);

    [Fact]
    public void Resolve_Should_Return_Null_When_No_Rows()
    {
        var result = RosterAssignmentResolver.Resolve([], EmployeeId, Target);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_Should_Ignore_Draft_Rows()
    {
        var draft = Roster(BaseShiftId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 20), Earlier);

        var result = RosterAssignmentResolver.Resolve([draft], EmployeeId, Target);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_Should_Return_Base_Published_Row_Covering_The_Date()
    {
        var baseRow = Published(Roster(BaseShiftId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 20), Earlier));

        var result = RosterAssignmentResolver.Resolve([baseRow], EmployeeId, Target);

        result.Should().Be(BaseShiftId);
    }

    [Fact]
    public void Resolve_Should_Prefer_Override_Over_Base()
    {
        var baseRow = Published(Roster(BaseShiftId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 20), Earlier));
        var overrideRow = Published(Roster(OverrideShiftId, Target, Target, Later, isOverride: true));

        var result = RosterAssignmentResolver.Resolve([baseRow, overrideRow], EmployeeId, Target);

        result.Should().Be(OverrideShiftId);
    }

    [Fact]
    public void Resolve_Should_Return_Null_When_No_Row_Covers_The_Date()
    {
        var baseRow = Published(Roster(BaseShiftId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), Earlier));

        var result = RosterAssignmentResolver.Resolve([baseRow], EmployeeId, Target);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_Should_Break_Ties_By_Most_Recently_Created()
    {
        var older = Published(Roster(BaseShiftId, Target, Target, Earlier));
        var newer = Published(Roster(OverrideShiftId, Target, Target, Later));

        var result = RosterAssignmentResolver.Resolve([older, newer], EmployeeId, Target);

        result.Should().Be(OverrideShiftId);
    }

    private static readonly DateTimeOffset Earlier = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    private static ShiftRoster Roster(ShiftId shiftId, DateOnly start, DateOnly end, DateTimeOffset createdAt, bool isOverride = false)
    {
        var period = DateRange.Create(start, end).Value;
        return ShiftRoster.Create(TenantId, EmployeeId, shiftId, period, createdAt, isOverride);
    }

    private static ShiftRoster Published(ShiftRoster roster)
    {
        roster.Publish(roster.CreatedAt, "hr@vespera.test");
        return roster;
    }
}
