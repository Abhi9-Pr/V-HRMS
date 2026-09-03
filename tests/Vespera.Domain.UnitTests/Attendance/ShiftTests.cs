using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Attendance;

public class ShiftTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConfigureBreak_Should_Set_BreakMinutes()
    {
        var shift = CreateShift();

        var result = shift.ConfigureBreak(45, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        shift.BreakMinutes.Should().Be(45);
    }

    [Fact]
    public void ConfigureBreak_Should_Reject_Negative_Minutes()
    {
        var shift = CreateShift();

        var result = shift.ConfigureBreak(-1, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        shift.BreakMinutes.Should().Be(0);
    }

    [Fact]
    public void Create_Should_Fail_When_Name_Is_Blank()
    {
        var result = Shift.Create(TenantId, "  ", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("shift.name_required");
    }

    [Fact]
    public void Create_Should_Fail_For_Negative_GraceMinutes()
    {
        var result = Shift.Create(TenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), -1, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("shift.invalid_grace");
    }

    [Fact]
    public void Rename_Should_Set_The_Trimmed_Name()
    {
        var shift = CreateShift();

        var result = shift.Rename("  Evening Shift  ", Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        shift.Name.Should().Be("Evening Shift");
    }

    [Fact]
    public void Rename_Should_Fail_When_Name_Is_Blank()
    {
        var shift = CreateShift();

        var result = shift.Rename("  ", Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("shift.name_required");
    }

    [Fact]
    public void Reschedule_Should_Update_Start_And_End_Time()
    {
        var shift = CreateShift();

        var result = shift.Reschedule(new TimeOnly(10, 0), new TimeOnly(19, 0), Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        shift.StartTime.Should().Be(new TimeOnly(10, 0));
        shift.EndTime.Should().Be(new TimeOnly(19, 0));
    }

    [Fact]
    public void IsOvernight_Should_Be_True_When_EndTime_Is_Before_StartTime()
    {
        var shift = CreateShift();
        shift.Reschedule(new TimeOnly(22, 0), new TimeOnly(6, 0), Now, "hr@vespera.test");

        shift.IsOvernight.Should().BeTrue();
    }

    [Fact]
    public void IsOvernight_Should_Be_False_For_A_Same_Day_Shift()
    {
        var shift = CreateShift();

        shift.IsOvernight.Should().BeFalse();
    }

    private static Shift CreateShift() =>
        Shift.Create(TenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, Now, "hr@vespera.test").Value;
}
