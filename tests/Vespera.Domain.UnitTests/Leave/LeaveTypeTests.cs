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
}
