using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Leave;

public class BlackoutPeriodTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Reject_A_Blank_Reason()
    {
        var period = DateRange.Create(new DateOnly(2026, 12, 20), new DateOnly(2027, 1, 2)).Value;

        var result = BlackoutPeriod.Create(TenantId.New(), period, "  ", leaveTypeId: null, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_Should_Be_True_For_Every_Leave_Type_When_Scoped_To_None()
    {
        var period = DateRange.Create(new DateOnly(2026, 12, 20), new DateOnly(2027, 1, 2)).Value;
        var blackout = BlackoutPeriod.Create(
            TenantId.New(), period, "Year-end freeze", leaveTypeId: null, Now, "hr@vespera.test").Value;

        blackout.AppliesTo(LeaveTypeId.New()).Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_Should_Be_Scoped_To_A_Single_Leave_Type_When_One_Is_Set()
    {
        var scopedType = LeaveTypeId.New();
        var period = DateRange.Create(new DateOnly(2026, 12, 20), new DateOnly(2027, 1, 2)).Value;
        var blackout = BlackoutPeriod.Create(
            TenantId.New(), period, "Freeze", leaveTypeId: scopedType, Now, "hr@vespera.test").Value;

        blackout.AppliesTo(scopedType).Should().BeTrue();
        blackout.AppliesTo(LeaveTypeId.New()).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_Should_Detect_An_Intersecting_Range()
    {
        var period = DateRange.Create(new DateOnly(2026, 12, 20), new DateOnly(2027, 1, 2)).Value;
        var blackout = BlackoutPeriod.Create(TenantId.New(), period, "Freeze", leaveTypeId: null, Now, "hr@vespera.test").Value;
        var requested = DateRange.Create(new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 5)).Value;

        blackout.Overlaps(requested).Should().BeTrue();
    }
}
