using Vespera.Domain.Common;

namespace Vespera.Domain.Attendance;

public readonly record struct ShiftId(Guid Value)
{
    public static ShiftId New() => new(Guid.NewGuid());
}

public sealed class Shift : AuditableTenantAggregateRoot<ShiftId>
{
    private Shift(
        ShiftId id, TenantId tenantId, string name, TimeOnly startTime, TimeOnly endTime, int graceMinutes,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        GraceMinutes = graceMinutes;
    }

    public string Name { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public int GraceMinutes { get; private set; }

    public bool IsOvernight => EndTime < StartTime;

    public static Result<Shift> Create(
        TenantId tenantId, string name, TimeOnly startTime, TimeOnly endTime, int graceMinutes,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Shift>(Error.Validation("shift.name_required", "Shift name is required."));
        }

        if (graceMinutes < 0)
        {
            return Result.Failure<Shift>(Error.Validation("shift.invalid_grace", "Grace minutes cannot be negative."));
        }

        return Result.Success(new Shift(ShiftId.New(), tenantId, name.Trim(), startTime, endTime, graceMinutes, occurredOn, createdBy));
    }

    public Result Rename(string name, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("shift.name_required", "Shift name is required."));
        }

        Name = name.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Reschedule(TimeOnly startTime, TimeOnly endTime, DateTimeOffset occurredOn, string modifiedBy)
    {
        StartTime = startTime;
        EndTime = endTime;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
