using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct PayrollReimbursementId(Guid Value)
{
    public static PayrollReimbursementId New() => new(Guid.NewGuid());
}

/// <summary>A finance-approved expense claim settled into this payroll run as a reimbursement
/// component. <see cref="SourceExpenseClaimId"/> is a plain Guid (not a typed Expense.ExpenseClaimId)
/// to avoid a Payroll -> Expense compile-time dependency between bounded contexts.</summary>
public sealed class PayrollReimbursement : Entity<PayrollReimbursementId>
{
    internal PayrollReimbursement(PayrollReimbursementId id, EmployeeId employeeId, Money amount, Guid sourceExpenseClaimId)
        : base(id)
    {
        EmployeeId = employeeId;
        Amount = amount;
        SourceExpenseClaimId = sourceExpenseClaimId;
    }

    public EmployeeId EmployeeId { get; }

    public Money Amount { get; }

    public Guid SourceExpenseClaimId { get; }
}
