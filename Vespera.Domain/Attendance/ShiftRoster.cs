using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Attendance;

public readonly record struct ShiftRosterId(Guid Value)
{
    public static ShiftRosterId New() => new(Guid.NewGuid());
}

public sealed class ShiftRoster : AggregateRoot<ShiftRosterId>, ITenantScoped
{
    private ShiftRoster(ShiftRosterId id, TenantId tenantId, EmployeeId employeeId, ShiftId shiftId, DateRange period)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        ShiftId = shiftId;
        Period = period;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public ShiftId ShiftId { get; private set; }

    public DateRange Period { get; }

    public static ShiftRoster Create(TenantId tenantId, EmployeeId employeeId, ShiftId shiftId, DateRange period) =>
        new(ShiftRosterId.New(), tenantId, employeeId, shiftId, period);

    public Result Reassign(ShiftId shiftId)
    {
        if (shiftId == ShiftId)
        {
            return Result.Failure(Error.Conflict("shift_roster.same_shift", "Employee is already on this shift."));
        }

        ShiftId = shiftId;
        return Result.Success();
    }
}
