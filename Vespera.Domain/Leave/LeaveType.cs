using Vespera.Domain.Common;

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
    }

    public string Name { get; private set; }

    public bool IsPaid { get; private set; }

    public decimal CarryForwardLimit { get; private set; }

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
}
