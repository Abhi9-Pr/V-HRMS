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

    [Fact]
    public void Create_Should_Populate_All_Fields()
    {
        var tenantId = TenantId.New();
        var leaveTypeId = LeaveTypeId.New();

        var result = LeavePolicy.Create(
            tenantId, leaveTypeId, annualEntitlementDays: 18m, accrualRatePerMonth: 1.5m,
            maxCarryForwardDays: 5m, new DateOnly(2026, 1, 1), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.LeaveTypeId.Should().Be(leaveTypeId);
        result.Value.AnnualEntitlementDays.Should().Be(18m);
        result.Value.AccrualRatePerMonth.Should().Be(1.5m);
        result.Value.MaxCarryForwardDays.Should().Be(5m);
    }

    [Fact]
    public void Create_Should_Reject_A_Negative_Annual_Entitlement()
    {
        var result = LeavePolicy.Create(
            TenantId.New(), LeaveTypeId.New(), annualEntitlementDays: -1m, accrualRatePerMonth: 1.5m,
            maxCarryForwardDays: 5m, new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_policy.invalid_entitlement");
    }

    [Fact]
    public void Create_Should_Reject_A_Negative_Accrual_Rate()
    {
        var result = LeavePolicy.Create(
            TenantId.New(), LeaveTypeId.New(), annualEntitlementDays: 18m, accrualRatePerMonth: -1m,
            maxCarryForwardDays: 5m, new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_policy.invalid_accrual_rate");
    }

    [Fact]
    public void EndOn_Should_Close_The_Policy()
    {
        var policy = CreatePolicy();

        var result = policy.EndOn(new DateOnly(2026, 12, 31));

        result.IsSuccess.Should().BeTrue();
        policy.ValidTo.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void ConfigureAccrual_Should_Reject_A_Negative_Minimum_Tenure()
    {
        var policy = CreatePolicy();

        var result = policy.ConfigureAccrual(AccrualFrequency.Monthly, minimumTenureMonthsForAccrual: -1, tenureAccrualTiers: []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_policy.invalid_minimum_tenure");
    }

    [Fact]
    public void ConfigureAccrual_Should_Reject_An_Invalid_Tenure_Tier()
    {
        var policy = CreatePolicy();

        var result = policy.ConfigureAccrual(
            AccrualFrequency.Monthly, minimumTenureMonthsForAccrual: 0,
            tenureAccrualTiers: [new TenureAccrualTier(-1, 1m)]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_policy.invalid_tenure_tier");
    }

    [Fact]
    public void ConfigureBalanceRules_Should_Set_The_Negative_Balance_Policy()
    {
        var policy = CreatePolicy();

        var result = policy.ConfigureBalanceRules(NegativeBalancePolicy.AllowNegative, maxNegativeBalanceDays: 5m, sandwichLeaveEnabled: true);

        result.IsSuccess.Should().BeTrue();
        policy.NegativeBalancePolicy.Should().Be(NegativeBalancePolicy.AllowNegative);
        policy.MaxNegativeBalanceDays.Should().Be(5m);
        policy.SandwichLeaveEnabled.Should().BeTrue();
    }

    [Fact]
    public void LeavePolicyId_New_Should_Generate_Distinct_Values()
    {
        LeavePolicyId.New().Should().NotBe(LeavePolicyId.New());
    }

    private static LeavePolicy CreatePolicy() =>
        LeavePolicy.Create(
            TenantId.New(), LeaveTypeId.New(), annualEntitlementDays: 18m, accrualRatePerMonth: 1.5m,
            maxCarryForwardDays: 5m, new DateOnly(2026, 1, 1), null).Value;
}
