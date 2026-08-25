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

    private static Shift CreateShift() =>
        Shift.Create(TenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, Now, "hr@vespera.test").Value;
}
