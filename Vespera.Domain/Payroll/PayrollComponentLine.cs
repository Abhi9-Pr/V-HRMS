using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct PayrollComponentLineId(Guid Value)
{
    public static PayrollComponentLineId New() => new(Guid.NewGuid());
}

public enum PayrollComponentDirection
{
    Earning,
    Deduction,
}

/// <summary>
/// One line item within one employee's payroll for one run — "Basic Salary: ₹40,000 (Earning)",
/// "PF Employee: ₹1,800 (Deduction)". This is the rules pipeline's actual unit of output; the
/// existing <see cref="PayrollLine"/> (Gross/Deductions/Net rollup, owned by <see cref="PayrollRun"/>)
/// is a projection summed from a set of these, not a hand-built input. Created directly by
/// <c>IPayrollComponentRule</c> implementations (in Vespera.Application), so the constructor is
/// public — unlike most entities in this codebase, this one has no aggregate root gatekeeping its
/// creation, since it's a computation result, not something a user submits.
/// </summary>
public sealed class PayrollComponentLine : Entity<PayrollComponentLineId>
{
    public PayrollComponentLine(
        SalaryComponentId componentId, string componentName, SalaryComponentType componentType,
        PayrollComponentDirection direction, Money amount)
        : this(PayrollComponentLineId.New(), componentId, componentName, componentType, direction, amount)
    {
    }

    public PayrollComponentLine(
        PayrollComponentLineId id, SalaryComponentId componentId, string componentName, SalaryComponentType componentType,
        PayrollComponentDirection direction, Money amount)
        : base(id)
    {
        if (amount.Amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "A payroll component line's amount cannot be negative — encode direction separately.");
        }

        ComponentId = componentId;
        ComponentName = componentName;
        ComponentType = componentType;
        Direction = direction;
        Amount = amount;
    }

    public SalaryComponentId ComponentId { get; }

    public string ComponentName { get; }

    public SalaryComponentType ComponentType { get; }

    public PayrollComponentDirection Direction { get; }

    public Money Amount { get; }

    public decimal SignedAmount => Direction == PayrollComponentDirection.Earning ? Amount.Amount : -Amount.Amount;
}
