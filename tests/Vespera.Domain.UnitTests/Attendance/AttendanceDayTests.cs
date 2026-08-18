using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Attendance;

public class AttendanceDayTests
{
    private static readonly DateTimeOffset MorningPunch = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EveningPunch = new(2026, 1, 15, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RecordPunch_Should_Fail_When_The_First_Punch_Is_Not_An_In_Punch()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));

        var result = day.RecordPunch(PunchType.Out, MorningPunch, location: null, PunchSource.Web);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordPunch_Should_Raise_PunchRecorded_And_Mark_Present()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));

        var result = day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);

        result.IsSuccess.Should().BeTrue();
        day.Status.Should().Be(AttendanceDayStatus.Present);
        day.DomainEvents.Should().ContainSingle(e => e is PunchRecorded);
    }

    [Fact]
    public void RecordPunch_Should_Reject_Two_Consecutive_In_Punches()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));
        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);

        var result = day.RecordPunch(PunchType.In, MorningPunch.AddMinutes(5), location: null, PunchSource.Web);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordPunch_Should_Reject_A_Punch_Before_The_Previous_One()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));
        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);

        var result = day.RecordPunch(PunchType.Out, MorningPunch.AddMinutes(-1), location: null, PunchSource.Web);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void WorkedTime_Should_Sum_Paired_In_Out_Punches()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));
        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);
        day.RecordPunch(PunchType.Out, EveningPunch, location: null, PunchSource.Web);

        day.WorkedTime.Should().Be(TimeSpan.FromHours(9));
    }
}
