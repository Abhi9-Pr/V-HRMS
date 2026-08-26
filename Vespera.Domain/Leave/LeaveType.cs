using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Leave;

public readonly record struct LeaveTypeId(Guid Value)
{
    public static LeaveTypeId New() => new(Guid.NewGuid());
}

public sealed class LeaveType : AuditableTenantAggregateRoot<LeaveTypeId>
{
    private LeaveType(
        LeaveTypeId id, TenantId tenantId, string name, bool isPaid, decimal carryForwardLimit,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        IsPaid = isPaid;
        CarryForwardLimit = carryForwardLimit;
        ApplicableGender = null;
        MinimumTenureMonths = 0;
        IsEncashable = false;
        MaxEncashableDays = 0m;
    }

    public string Name { get; private set; }

    public bool IsPaid { get; private set; }

    public decimal CarryForwardLimit { get; private set; }

    /// <summary>Null = open to every gender. Set for e.g. maternity/paternity leave.</summary>
    public Gender? ApplicableGender { get; private set; }

    /// <summary>An employee must have at least this many months of tenure to request this leave
    /// type. 0 = no restriction.</summary>
    public int MinimumTenureMonths { get; private set; }

    public bool IsEncashable { get; private set; }

    /// <summary>Only meaningful when <see cref="IsEncashable"/> is true — the most days an employee
    /// may encash from their balance for this type in one go.</summary>
    public decimal MaxEncashableDays { get; private set; }

    public static Result<LeaveType> Create(
        TenantId tenantId, string name, bool isPaid, decimal carryForwardLimit, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<LeaveType>(Error.Validation("leave_type.name_required", "Leave type name is required."));
        }

        if (carryForwardLimit < 0)
        {
            return Result.Failure<LeaveType>(Error.Validation("leave_type.invalid_carry_forward", "Carry forward limit cannot be negative."));
        }

        return Result.Success(new LeaveType(LeaveTypeId.New(), tenantId, name.Trim(), isPaid, carryForwardLimit, occurredOn, createdBy));
    }

    public Result Rename(string name, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("leave_type.name_required", "Leave type name is required."));
        }

        Name = name.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result UpdateAccrualRules(bool isPaid, decimal carryForwardLimit, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (carryForwardLimit < 0)
        {
            return Result.Failure(Error.Validation("leave_type.invalid_carry_forward", "Carry forward limit cannot be negative."));
        }

        IsPaid = isPaid;
        CarryForwardLimit = carryForwardLimit;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result UpdateEligibilityRules(
        Gender? applicableGender, int minimumTenureMonths, bool isEncashable, decimal maxEncashableDays,
        DateTimeOffset occurredOn, string modifiedBy)
    {
        if (minimumTenureMonths < 0)
        {
            return Result.Failure(Error.Validation("leave_type.invalid_minimum_tenure", "Minimum tenure cannot be negative."));
        }

        if (maxEncashableDays < 0)
        {
            return Result.Failure(Error.Validation("leave_type.invalid_max_encashable", "Max encashable days cannot be negative."));
        }

        ApplicableGender = applicableGender;
        MinimumTenureMonths = minimumTenureMonths;
        IsEncashable = isEncashable;
        MaxEncashableDays = maxEncashableDays;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
