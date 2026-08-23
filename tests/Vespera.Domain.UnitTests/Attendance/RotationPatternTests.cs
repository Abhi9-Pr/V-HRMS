using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Attendance;

public class RotationPatternTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_With_Contiguous_Zero_Based_Sequence()
    {
        var shiftId = ShiftId.New();
        var days = new List<RotationPatternDay>
        {
            new(0, shiftId),
            new(1, shiftId),
            new(2, null),
        };

        var result = RotationPattern.Create(TenantId, "3-Day Rotation", days, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Days.Should().HaveCount(3);
        result.Value.Days[2].ShiftId.Should().BeNull();
    }

    [Fact]
    public void Create_Should_Fail_With_No_Days()
    {
        var result = RotationPattern.Create(TenantId, "Empty", [], Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Fail_With_A_Gap_In_The_Sequence()
    {
        var days = new List<RotationPatternDay> { new(0, ShiftId.New()), new(2, null) };

        var result = RotationPattern.Create(TenantId, "Gappy", days, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Fail_With_A_Duplicate_Sequence_Number()
    {
        var days = new List<RotationPatternDay> { new(0, ShiftId.New()), new(0, null) };

        var result = RotationPattern.Create(TenantId, "Duplicate", days, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Fail_With_A_Blank_Name()
    {
        var result = RotationPattern.Create(TenantId, "  ", [new RotationPatternDay(0, null)], Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reconfigure_Should_Replace_The_Day_List()
    {
        var pattern = RotationPattern.Create(
            TenantId, "Pattern", [new RotationPatternDay(0, ShiftId.New())], Now, "hr@vespera.test").Value;
        var newShiftId = ShiftId.New();

        var result = pattern.Reconfigure(
            [new RotationPatternDay(0, newShiftId), new RotationPatternDay(1, null)], Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        pattern.Days.Should().HaveCount(2);
        pattern.Days[0].ShiftId.Should().Be(newShiftId);
    }

    [Fact]
    public void Reconfigure_Should_Reject_Invalid_Sequence_And_Leave_The_Original_Days_Intact()
    {
        var pattern = RotationPattern.Create(
            TenantId, "Pattern", [new RotationPatternDay(0, ShiftId.New())], Now, "hr@vespera.test").Value;

        var result = pattern.Reconfigure([new RotationPatternDay(5, null)], Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        pattern.Days.Should().HaveCount(1);
        pattern.Days[0].SequenceNumber.Should().Be(0);
    }
}
