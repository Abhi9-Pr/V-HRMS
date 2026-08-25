using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Services;

namespace Vespera.Domain.UnitTests.Services;

public class AttendanceDayCalculatorTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

    private static Shift DayShift(int graceMinutes = 10, int breakMinutes = 0)
    {
        var shift = Shift.Create(TenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), graceMinutes, Now, "hr@vespera.test").Value;
        if (breakMinutes > 0)
        {
            shift.ConfigureBreak(breakMinutes, Now, "hr@vespera.test");
        }

        return shift;
    }

    private static Shift NightShift(int graceMinutes = 10) =>
        Shift.Create(TenantId, "Night Shift", new TimeOnly(22, 0), new TimeOnly(6, 0), graceMinutes, Now, "hr@vespera.test").Value;

    private static AttendancePunch In(DateTimeOffset at) => new(AttendancePunchId.New(), PunchType.In, at, null, PunchSource.Web);

    private static AttendancePunch Out(DateTimeOffset at) => new(AttendancePunchId.New(), PunchType.Out, at, null, PunchSource.Web);

    [Fact]
    public void Compute_Should_Report_Clean_WorkedMinutes_For_A_Normal_Same_Day_Shift()
    {
        var shift = DayShift();
        var punches = new List<AttendancePunch>
        {
            In(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)),
            Out(new DateTimeOffset(2026, 1, 15, 18, 0, 0, TimeSpan.Zero)),
        };

        var result = AttendanceDayCalculator.Compute(
            punches, shift, isHoliday: false, isWeekOff: false, new TimeOnly(9, 0), new TimeOnly(18, 0));

        result.WorkedMinutes.Should().Be(9 * 60);
        result.LateByMinutes.Should().Be(0);
        result.EarlyLeaveByMinutes.Should().Be(0);
        result.OvertimeMinutes.Should().Be(0);
        result.Status.Should().Be(AttendanceDayStatus.Present);
    }

    [Fact]
    public void Compute_Should_Correctly_Pair_A_Night_Shifts_Punches_Crossing_Midnight()
    {
        var shift = NightShift();
        // In at 22:05 on day 1, Out at 06:10 the NEXT calendar day -- the calculator has no
        // date-boundary logic at all, so it must not treat this as two fragments or produce a
        // negative/nonsensical duration; it just pairs chronologically.
        var punches = new List<AttendancePunch>
        {
            In(new DateTimeOffset(2026, 1, 15, 22, 5, 0, TimeSpan.Zero)),
            Out(new DateTimeOffset(2026, 1, 16, 6, 10, 0, TimeSpan.Zero)),
        };

        var result = AttendanceDayCalculator.Compute(
            punches, shift, isHoliday: false, isWeekOff: false, new TimeOnly(22, 5), new TimeOnly(6, 10));

        result.WorkedMinutes.Should().Be(8 * 60 + 5); // 22:05 -> 06:10 = 8h05m
        result.WorkedMinutes.Should().BePositive();
        result.OvertimeMinutes.Should().Be(10); // 10 minutes past the 06:00 scheduled end
        result.Status.Should().Be(AttendanceDayStatus.Present);
    }

    [Fact]
    public void Compute_Should_Deduct_The_Shifts_BreakMinutes_From_WorkedMinutes()
    {
        var shift = DayShift(breakMinutes: 30);
        var punches = new List<AttendancePunch>
        {
            In(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)),
            Out(new DateTimeOffset(2026, 1, 15, 18, 0, 0, TimeSpan.Zero)),
        };

        var result = AttendanceDayCalculator.Compute(
            punches, shift, isHoliday: false, isWeekOff: false, new TimeOnly(9, 0), new TimeOnly(18, 0));

        result.WorkedMinutes.Should().Be(9 * 60 - 30);
    }

    [Fact]
    public void Compute_Should_Report_LateByMinutes_Beyond_Grace()
    {
        var shift = DayShift(graceMinutes: 10);

        var result = AttendanceDayCalculator.Compute(
            [], shift, isHoliday: false, isWeekOff: false, firstInLocalTime: new TimeOnly(9, 25), lastOutLocalTime: null);

        result.LateByMinutes.Should().Be(25);
    }

    [Fact]
    public void Compute_Should_Not_Report_Late_Within_Grace()
    {
        var shift = DayShift(graceMinutes: 10);

        var result = AttendanceDayCalculator.Compute(
            [], shift, isHoliday: false, isWeekOff: false, firstInLocalTime: new TimeOnly(9, 5), lastOutLocalTime: null);

        result.LateByMinutes.Should().Be(0);
    }

    [Fact]
    public void Compute_Should_Report_EarlyLeaveByMinutes_When_LastOut_Precedes_ShiftEnd()
    {
        var shift = DayShift();

        var result = AttendanceDayCalculator.Compute(
            [], shift, isHoliday: false, isWeekOff: false, firstInLocalTime: null, lastOutLocalTime: new TimeOnly(17, 30));

        result.EarlyLeaveByMinutes.Should().Be(30);
    }

    [Fact]
    public void Compute_Should_Report_OvertimeMinutes_When_LastOut_Follows_ShiftEnd()
    {
        var shift = DayShift();

        var result = AttendanceDayCalculator.Compute(
            [], shift, isHoliday: false, isWeekOff: false, firstInLocalTime: null, lastOutLocalTime: new TimeOnly(19, 0));

        result.OvertimeMinutes.Should().Be(60);
    }

    [Fact]
    public void Compute_Should_Flag_LopCandidate_When_No_Punches_On_A_Scheduled_Working_Day()
    {
        var shift = DayShift();

        var result = AttendanceDayCalculator.Compute([], shift, isHoliday: false, isWeekOff: false, null, null);

        result.IsLopCandidate.Should().BeTrue();
        result.Status.Should().Be(AttendanceDayStatus.Absent);
    }

    [Fact]
    public void Compute_Should_Not_Flag_LopCandidate_On_A_Holiday()
    {
        var shift = DayShift();

        var result = AttendanceDayCalculator.Compute([], shift, isHoliday: true, isWeekOff: false, null, null);

        result.IsLopCandidate.Should().BeFalse();
        result.Status.Should().Be(AttendanceDayStatus.Holiday);
    }

    [Fact]
    public void Compute_Should_Not_Flag_LopCandidate_On_A_WeekOff()
    {
        var shift = DayShift();

        var result = AttendanceDayCalculator.Compute([], shift, isHoliday: false, isWeekOff: true, null, null);

        result.IsLopCandidate.Should().BeFalse();
        result.Status.Should().Be(AttendanceDayStatus.WeekOff);
    }

    [Fact]
    public void Compute_Should_Not_Flag_LopCandidate_When_No_Shift_Is_Assigned()
    {
        var result = AttendanceDayCalculator.Compute([], assignedShift: null, isHoliday: false, isWeekOff: false, null, null);

        result.IsLopCandidate.Should().BeFalse();
        result.Status.Should().Be(AttendanceDayStatus.Absent);
    }

    [Fact]
    public void Compute_Should_Report_HalfDay_When_WorkedMinutes_Is_Under_Half_The_Scheduled_Duration()
    {
        var shift = DayShift(); // 9h scheduled
        var punches = new List<AttendancePunch>
        {
            In(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)),
            Out(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero)), // 3h worked, < 4.5h half
        };

        var result = AttendanceDayCalculator.Compute(
            punches, shift, isHoliday: false, isWeekOff: false, new TimeOnly(9, 0), new TimeOnly(12, 0));

        result.Status.Should().Be(AttendanceDayStatus.HalfDay);
    }
}
