using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Domain.UnitTests.Leave;

public class LeavePolicyTests
{
    [Fact]
    public void ConfigureApprovalChain_Should_Record_SkipLevel_And_Hr_Requirements()
    {
        var policy = CreatePolicy();

        var result = policy.ConfigureApprovalChain(requiresSkipLevelApproval: true, skipLevelThresholdDays: 5m, requiresHrApproval: true);

        result.IsSuccess.Should().BeTrue();
        policy.RequiresSkipLevelApproval.Should().BeTrue();
        policy.SkipLevelThresholdDays.Should().Be(5m);
        policy.RequiresHrApproval.Should().BeTrue();
    }

    [Fact]
    public void ConfigureApprovalChain_Should_Reject_A_Negative_Threshold()
    {
        var policy = CreatePolicy();

        var result = policy.ConfigureApprovalChain(requiresSkipLevelApproval: true, skipLevelThresholdDays: -1m, requiresHrApproval: false);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ConfigureBalanceRules_Should_Reject_A_Negative_Cap()
    {
        var policy = CreatePolicy();

        var result = policy.ConfigureBalanceRules(NegativeBalancePolicy.AllowNegative, maxNegativeBalanceDays: -2m, sandwichLeaveEnabled: false);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ConfigureAccrual_Should_Sort_Tenure_Tiers_By_Minimum_Tenure()
    {
        var policy = CreatePolicy();

        policy.ConfigureAccrual(
            AccrualFrequency.Monthly, minimumTenureMonthsForAccrual: 0,
            tenureAccrualTiers: [new TenureAccrualTier(24, 2m), new TenureAccrualTier(0, 1m)]);

        policy.TenureAccrualTiers.Select(t => t.MinimumTenureMonths).Should().Equal(0, 24);
    }

    private static LeavePolicy CreatePolicy() =>
        LeavePolicy.Create(
            TenantId.New(), LeaveTypeId.New(), annualEntitlementDays: 18m, accrualRatePerMonth: 1.5m,
            maxCarryForwardDays: 5m, new DateOnly(2026, 1, 1), null).Value;
}
