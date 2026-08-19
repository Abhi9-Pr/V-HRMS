using Vespera.Domain.Common;

namespace Vespera.Domain.Leave;

public readonly record struct LeavePolicyId(Guid Value)
{
    public static LeavePolicyId New() => new(Guid.NewGuid());
}

public sealed class LeavePolicy : EffectiveDated<LeavePolicyId>, ITenantScoped
{
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
    }

    public TenantId TenantId { get; }

    public LeaveTypeId LeaveTypeId { get; }

    public decimal AnnualEntitlementDays { get; }

    public decimal AccrualRatePerMonth { get; }

    public decimal MaxCarryForwardDays { get; }

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
}
