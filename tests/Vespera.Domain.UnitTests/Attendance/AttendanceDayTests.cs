using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;

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

    [Fact]
    public void RecordPunch_Should_Persist_The_Flag_Reason_When_Supplied()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));

        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Mobile, requiresApproval: true, flagReason: "low_gps_accuracy");

        var punch = day.Punches.Single();
        punch.RequiresApproval.Should().BeTrue();
        punch.FlagReason.Should().Be("low_gps_accuracy");
    }

    [Fact]
    public void RecordPunch_Should_Default_To_Not_Flagged()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));

        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);

        var punch = day.Punches.Single();
        punch.RequiresApproval.Should().BeFalse();
        punch.FlagReason.Should().BeNull();
    }

    [Fact]
    public void ClearPunchFlag_Should_Reset_The_Flag_On_The_Matching_Punch()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));
        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Mobile, requiresApproval: true, flagReason: "impossible_travel");
        var punchId = day.Punches.Single().Id;

        var result = day.ClearPunchFlag(punchId);

        result.IsSuccess.Should().BeTrue();
        var punch = day.Punches.Single();
        punch.RequiresApproval.Should().BeFalse();
        punch.FlagReason.Should().BeNull();
    }

    [Fact]
    public void ClearPunchFlag_Should_Fail_For_An_Unknown_Punch_Id()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));
        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);

        var result = day.ClearPunchFlag(AttendancePunchId.New());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ApplyComputation_Should_Store_The_Computed_Fields_And_Be_Idempotent()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));
        var result = new AttendanceComputationResult(
            MorningPunch, EveningPunch, WorkedMinutes: 480, LateByMinutes: 5, EarlyLeaveByMinutes: 0,
            OvertimeMinutes: 15, IsLopCandidate: false, AttendanceDayStatus.Present);

        day.ApplyComputation(result, EveningPunch, "system");
        day.ApplyComputation(result, EveningPunch, "system");

        day.FirstIn.Should().Be(MorningPunch);
        day.LastOut.Should().Be(EveningPunch);
        day.WorkedMinutes.Should().Be(480);
        day.LateByMinutes.Should().Be(5);
        day.OvertimeMinutes.Should().Be(15);
        day.IsLopCandidate.Should().BeFalse();
        day.Status.Should().Be(AttendanceDayStatus.Present);
    }

    [Fact]
    public void IsOpen_Should_Be_True_Only_When_The_Last_Punch_Is_An_Unmatched_In()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));
        day.IsOpen.Should().BeFalse();

        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);
        day.IsOpen.Should().BeTrue();

        day.RecordPunch(PunchType.Out, EveningPunch, location: null, PunchSource.Web);
        day.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_Should_Always_Be_False()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));

        day.IsDeleted.Should().BeFalse();
        day.DeletedAt.Should().BeNull();
        day.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void RecordPunch_Should_Update_LastChangedAt_To_The_Punch_Time()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));

        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);
        day.LastChangedAt.Should().Be(MorningPunch);

        day.RecordPunch(PunchType.Out, EveningPunch, location: null, PunchSource.Web);
        day.LastChangedAt.Should().Be(EveningPunch);
    }

    [Fact]
    public void ApplyComputation_Should_Update_LastChangedAt_To_The_Recompute_Time()
    {
        var day = AttendanceDay.Open(TenantId.New(), EmployeeId.New(), new DateOnly(2026, 1, 15));
        day.RecordPunch(PunchType.In, MorningPunch, location: null, PunchSource.Web);
        day.RecordPunch(PunchType.Out, EveningPunch, location: null, PunchSource.Web);
        var recomputedAt = EveningPunch.AddDays(1);
        var result = new AttendanceComputationResult(
            MorningPunch, EveningPunch, WorkedMinutes: 480, LateByMinutes: 0, EarlyLeaveByMinutes: 0,
            OvertimeMinutes: 0, IsLopCandidate: false, AttendanceDayStatus.Present);

        day.ApplyComputation(result, recomputedAt, "system");

        day.LastChangedAt.Should().Be(recomputedAt);
    }
}
