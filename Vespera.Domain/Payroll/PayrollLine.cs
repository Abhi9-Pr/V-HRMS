using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct PayrollLineId(Guid Value)
{
    public static PayrollLineId New() => new(Guid.NewGuid());
}

public sealed class PayrollLine : Entity<PayrollLineId>
{
    internal PayrollLine(PayrollLineId id, EmployeeId employeeId, Money gross, Money deductions, Money net, decimal lossOfPayDays)
        : base(id)
    {
        EmployeeId = employeeId;
        Gross = gross;
        Deductions = deductions;
        Net = net;
        LossOfPayDays = lossOfPayDays;
    }

    public EmployeeId EmployeeId { get; }

    public Money Gross { get; }

    public Money Deductions { get; }

    public Money Net { get; }

    public decimal LossOfPayDays { get; }
}
