using Vespera.Domain.Common;

namespace Vespera.Domain.Leave;

public readonly record struct LeavePolicyId(Guid Value)
{
    public static LeavePolicyId New() => new(Guid.NewGuid());
}

public enum AccrualFrequency
{
    Monthly,
    Annual,
}

public enum NegativeBalancePolicy
{
    /// <summary>A request that would exceed the available balance is rejected outright — no LOP routing.</summary>
    NotAllowed,

    /// <summary>The default: the excess over the available balance routes to loss-of-pay, given
    /// explicit acknowledgement — the paid portion never goes negative.</summary>
    AllowWithLop,

    /// <summary>The balance may go negative, up to <see cref="LeavePolicy.MaxNegativeBalanceDays"/>,
    /// without routing anything to loss-of-pay.</summary>
    AllowNegative,
}

/// <summary>One tenure-based accrual rate — e.g. "1.5 days/month once 24 months in." An empty tier
/// list on a <see cref="LeavePolicy"/> means the flat <see cref="LeavePolicy.AccrualRatePerMonth"/>
/// applies at every tenure. A reference type (not a struct), even though it carries no identity of
/// its own, so EF Core can map <see cref="LeavePolicy.TenureAccrualTiers"/> as an owned collection —
/// the same technique <c>AttendanceDay.Punches</c> and <c>ApprovalChain.Steps</c> use.</summary>
public sealed record TenureAccrualTier(int MinimumTenureMonths, decimal MonthlyRate);

public sealed class LeavePolicy : EffectiveDated<LeavePolicyId>, ITenantScoped
{
    private readonly List<TenureAccrualTier> _tenureAccrualTiers = [];

    private LeavePolicy(
        LeavePolicyId id, TenantId tenantId, LeaveTypeId leaveTypeId, decimal annualEntitlementDays,
        decimal accrualRatePerMonth, decimal maxCarryForwardDays, DateOnly validFrom, DateOnly? validTo)
        : base(id, validFrom, validTo)
    {
        TenantId = tenantId;
        LeaveTypeId = leaveTypeId;
        AnnualEntitlementDays = annualEntitlementDays;
        AccrualRatePerMonth = accrualRatePerMonth;
        MaxCarryForwardDays = maxCarryForwardDays;
        AccrualFrequency = AccrualFrequency.Monthly;
        MinimumTenureMonthsForAccrual = 0;
        RequiresSkipLevelApproval = false;
        SkipLevelThresholdDays = null;
        RequiresHrApproval = false;
        NegativeBalancePolicy = NegativeBalancePolicy.AllowWithLop;
        MaxNegativeBalanceDays = 0m;
        SandwichLeaveEnabled = false;
    }

    public TenantId TenantId { get; }

    public LeaveTypeId LeaveTypeId { get; }

    public decimal AnnualEntitlementDays { get; }

    public decimal AccrualRatePerMonth { get; }

    public decimal MaxCarryForwardDays { get; }

    public AccrualFrequency AccrualFrequency { get; private set; }

    /// <summary>An employee accrues nothing under this policy until their tenure (as of the accrual
    /// date) reaches this many months. 0 = accrual starts immediately on joining.</summary>
    public int MinimumTenureMonthsForAccrual { get; private set; }

    public IReadOnlyList<TenureAccrualTier> TenureAccrualTiers => _tenureAccrualTiers.AsReadOnly();

    /// <summary>Tier 2 of the approval chain (the requester's skip-level manager) is included
    /// whenever this is true, or the request's day count exceeds <see cref="SkipLevelThresholdDays"/>.</summary>
    public bool RequiresSkipLevelApproval { get; private set; }

    public decimal? SkipLevelThresholdDays { get; private set; }

    /// <summary>Adds a final HR tier to the approval chain, after the manager tier(s).</summary>
    public bool RequiresHrApproval { get; private set; }

    public NegativeBalancePolicy NegativeBalancePolicy { get; private set; }

    /// <summary>Only meaningful when <see cref="NegativeBalancePolicy"/> is
    /// <see cref="Leave.NegativeBalancePolicy.AllowNegative"/> — the floor <see cref="LeaveBalance.Available"/>
    /// may reach, expressed as a positive number of days (e.g. 5 means balance may go to -5).</summary>
    public decimal MaxNegativeBalanceDays { get; private set; }

    public bool SandwichLeaveEnabled { get; private set; }

    public static Result<LeavePolicy> Create(
        TenantId tenantId, LeaveTypeId leaveTypeId, decimal annualEntitlementDays, decimal accrualRatePerMonth,
        decimal maxCarryForwardDays, DateOnly validFrom, DateOnly? validTo)
    {
        if (annualEntitlementDays < 0)
        {
            return Result.Failure<LeavePolicy>(
                Error.Validation("leave_policy.invalid_entitlement", "Annual entitlement cannot be negative."));
        }

        if (accrualRatePerMonth < 0)
        {
            return Result.Failure<LeavePolicy>(
                Error.Validation("leave_policy.invalid_accrual_rate", "Accrual rate cannot be negative."));
        }

        return Result.Success(new LeavePolicy(
            LeavePolicyId.New(), tenantId, leaveTypeId, annualEntitlementDays, accrualRatePerMonth, maxCarryForwardDays, validFrom, validTo));
    }

    public Result EndOn(DateOnly validTo) => Close(validTo);

    public Result ConfigureAccrual(
        AccrualFrequency frequency, int minimumTenureMonthsForAccrual, IReadOnlyList<TenureAccrualTier> tenureAccrualTiers)
    {
        if (minimumTenureMonthsForAccrual < 0)
        {
            return Result.Failure(Error.Validation(
                "leave_policy.invalid_minimum_tenure", "Minimum tenure for accrual cannot be negative."));
        }

        if (tenureAccrualTiers.Any(t => t.MinimumTenureMonths < 0 || t.MonthlyRate < 0))
        {
            return Result.Failure(Error.Validation(
                "leave_policy.invalid_tenure_tier", "A tenure tier's minimum tenure and rate must both be non-negative."));
        }

        AccrualFrequency = frequency;
        MinimumTenureMonthsForAccrual = minimumTenureMonthsForAccrual;
        _tenureAccrualTiers.Clear();
        _tenureAccrualTiers.AddRange(tenureAccrualTiers.OrderBy(t => t.MinimumTenureMonths));
        return Result.Success();
    }

    public Result ConfigureApprovalChain(bool requiresSkipLevelApproval, decimal? skipLevelThresholdDays, bool requiresHrApproval)
    {
        if (skipLevelThresholdDays is < 0)
        {
            return Result.Failure(Error.Validation(
                "leave_policy.invalid_skip_level_threshold", "The skip-level threshold cannot be negative."));
        }

        RequiresSkipLevelApproval = requiresSkipLevelApproval;
        SkipLevelThresholdDays = skipLevelThresholdDays;
        RequiresHrApproval = requiresHrApproval;
        return Result.Success();
    }

    public Result ConfigureBalanceRules(NegativeBalancePolicy negativeBalancePolicy, decimal maxNegativeBalanceDays, bool sandwichLeaveEnabled)
    {
        if (maxNegativeBalanceDays < 0)
        {
            return Result.Failure(Error.Validation(
                "leave_policy.invalid_max_negative_balance", "The negative-balance cap cannot be negative."));
        }

        NegativeBalancePolicy = negativeBalancePolicy;
        MaxNegativeBalanceDays = maxNegativeBalanceDays;
        SandwichLeaveEnabled = sandwichLeaveEnabled;
        return Result.Success();
    }
}
