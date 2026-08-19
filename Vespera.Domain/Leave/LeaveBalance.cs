using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Leave;

public readonly record struct LeaveBalanceId(Guid Value)
{
    public static LeaveBalanceId New() => new(Guid.NewGuid());
}

public sealed class LeaveBalance : AggregateRoot<LeaveBalanceId>, ITenantScoped
{
    private LeaveBalance(LeaveBalanceId id, TenantId tenantId, EmployeeId employeeId, LeaveTypeId leaveTypeId, int year)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        Year = year;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public LeaveTypeId LeaveTypeId { get; }

    public int Year { get; }

    public decimal Accrued { get; private set; }

    public decimal Used { get; private set; }

    public decimal CarriedForward { get; private set; }

    public decimal Available => Accrued + CarriedForward - Used;

    public static LeaveBalance Open(TenantId tenantId, EmployeeId employeeId, LeaveTypeId leaveTypeId, int year, decimal carriedForward = 0m)
    {
        var balance = new LeaveBalance(LeaveBalanceId.New(), tenantId, employeeId, leaveTypeId, year)
        {
            CarriedForward = carriedForward,
        };

        return balance;
    }

    public Result Accrue(decimal days)
    {
        if (days <= 0)
        {
            return Result.Failure(Error.Validation("leave_balance.invalid_accrual", "Accrual amount must be positive."));
        }

        Accrued += days;
        return Result.Success();
    }

    public Result Deduct(decimal days)
    {
        if (days <= 0)
        {
            return Result.Failure(Error.Validation("leave_balance.invalid_deduction", "Deduction amount must be positive."));
        }

        Used += days;
        return Result.Success();
    }
}
