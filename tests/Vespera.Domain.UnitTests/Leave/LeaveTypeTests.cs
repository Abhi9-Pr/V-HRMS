using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Domain.UnitTests.Leave;

public class LeaveTypeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdateEligibilityRules_Should_Record_Gender_Tenure_And_Encashment_Rules()
    {
        var leaveType = LeaveType.Create(TenantId.New(), "Maternity Leave", isPaid: true, carryForwardLimit: 0, Now, "hr@vespera.test").Value;

        var result = leaveType.UpdateEligibilityRules(
            applicableGender: Gender.Female, minimumTenureMonths: 3, isEncashable: false, maxEncashableDays: 0, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        leaveType.ApplicableGender.Should().Be(Gender.Female);
        leaveType.MinimumTenureMonths.Should().Be(3);
        leaveType.IsEncashable.Should().BeFalse();
    }

    [Fact]
    public void UpdateEligibilityRules_Should_Reject_A_Negative_Minimum_Tenure()
    {
        var leaveType = LeaveType.Create(TenantId.New(), "Earned Leave", isPaid: true, carryForwardLimit: 15, Now, "hr@vespera.test").Value;

        var result = leaveType.UpdateEligibilityRules(
            applicableGender: null, minimumTenureMonths: -1, isEncashable: true, maxEncashableDays: 5, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateEligibilityRules_Should_Reject_A_Negative_MaxEncashableDays()
    {
        var leaveType = LeaveType.Create(TenantId.New(), "Earned Leave", isPaid: true, carryForwardLimit: 15, Now, "hr@vespera.test").Value;

        var result = leaveType.UpdateEligibilityRules(
            applicableGender: null, minimumTenureMonths: 0, isEncashable: true, maxEncashableDays: -5, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_type.invalid_max_encashable");
    }

    [Fact]
    public void Create_Should_Fail_With_A_Blank_Name()
    {
        var result = LeaveType.Create(TenantId.New(), "  ", isPaid: true, carryForwardLimit: 0, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_type.name_required");
    }

    [Fact]
    public void Create_Should_Fail_With_A_Negative_CarryForwardLimit()
    {
        var result = LeaveType.Create(TenantId.New(), "Earned Leave", isPaid: true, carryForwardLimit: -1, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_type.invalid_carry_forward");
    }

    [Fact]
    public void Rename_Should_Update_The_Name()
    {
        var leaveType = LeaveType.Create(TenantId.New(), "Earned Leave", isPaid: true, carryForwardLimit: 15, Now, "hr@vespera.test").Value;

        var result = leaveType.Rename("Annual Leave", Now.AddMinutes(1), "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        leaveType.Name.Should().Be("Annual Leave");
    }

    [Fact]
    public void Rename_Should_Fail_With_A_Blank_Name()
    {
        var leaveType = LeaveType.Create(TenantId.New(), "Earned Leave", isPaid: true, carryForwardLimit: 15, Now, "hr@vespera.test").Value;

        var result = leaveType.Rename("   ", Now.AddMinutes(1), "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_type.name_required");
    }

    [Fact]
    public void UpdateAccrualRules_Should_Update_IsPaid_And_CarryForwardLimit()
    {
        var leaveType = LeaveType.Create(TenantId.New(), "Earned Leave", isPaid: true, carryForwardLimit: 15, Now, "hr@vespera.test").Value;

        var result = leaveType.UpdateAccrualRules(isPaid: false, carryForwardLimit: 10, Now.AddMinutes(1), "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        leaveType.IsPaid.Should().BeFalse();
        leaveType.CarryForwardLimit.Should().Be(10);
    }

    [Fact]
    public void UpdateAccrualRules_Should_Reject_A_Negative_CarryForwardLimit()
    {
        var leaveType = LeaveType.Create(TenantId.New(), "Earned Leave", isPaid: true, carryForwardLimit: 15, Now, "hr@vespera.test").Value;

        var result = leaveType.UpdateAccrualRules(isPaid: true, carryForwardLimit: -1, Now.AddMinutes(1), "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_type.invalid_carry_forward");
    }

    [Fact]
    public void LeaveTypeId_New_Should_Generate_Distinct_Values()
    {
        LeaveTypeId.New().Should().NotBe(LeaveTypeId.New());
    }
}
