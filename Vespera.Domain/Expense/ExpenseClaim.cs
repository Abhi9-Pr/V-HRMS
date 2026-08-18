using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Expense;

public readonly record struct ExpenseClaimId(Guid Value)
{
    public static ExpenseClaimId New() => new(Guid.NewGuid());
}

public enum ExpenseClaimStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Reimbursed,
}

public sealed class ExpenseClaim : AggregateRoot<ExpenseClaimId>, ITenantScoped
{
    private readonly List<ExpenseLine> _lines = [];

    private ExpenseClaim(ExpenseClaimId id, TenantId tenantId, EmployeeId employeeId)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Status = ExpenseClaimStatus.Draft;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public ExpenseClaimStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public IReadOnlyCollection<ExpenseLine> Lines => _lines.AsReadOnly();

    public static ExpenseClaim Open(TenantId tenantId, EmployeeId employeeId) => new(ExpenseClaimId.New(), tenantId, employeeId);

    public Money Total(Currency currency)
    {
        var total = Money.Zero(currency);

        foreach (var line in _lines)
        {
            total += line.Amount;
        }

        return total;
    }

    public Result AddLine(string category, Money amount, DateOnly expenseDate, string? receiptReference)
    {
        if (Status != ExpenseClaimStatus.Draft)
        {
            return Result.Failure(Error.Conflict("expense_claim.not_draft", "Lines can only be added while the claim is in Draft."));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return Result.Failure(Error.Validation("expense_claim.category_required", "Category is required."));
        }

        _lines.Add(new ExpenseLine(ExpenseLineId.New(), category.Trim(), amount, expenseDate, receiptReference));
        return Result.Success();
    }

    public Result Submit()
    {
        if (Status != ExpenseClaimStatus.Draft)
        {
            return Result.Failure(Error.Conflict("expense_claim.not_draft", "Only a draft claim can be submitted."));
        }

        if (_lines.Count == 0)
        {
            return Result.Failure(Error.Validation("expense_claim.empty", "Cannot submit a claim with no lines."));
        }

        Status = ExpenseClaimStatus.Submitted;
        return Result.Success();
    }

    public Result Approve()
    {
        if (Status != ExpenseClaimStatus.Submitted)
        {
            return Result.Failure(Error.Conflict("expense_claim.not_submitted", "Only a submitted claim can be approved."));
        }

        Status = ExpenseClaimStatus.Approved;
        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (Status != ExpenseClaimStatus.Submitted)
        {
            return Result.Failure(Error.Conflict("expense_claim.not_submitted", "Only a submitted claim can be rejected."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("expense_claim.rejection_reason_required", "Rejection reason is required."));
        }

        Status = ExpenseClaimStatus.Rejected;
        RejectionReason = reason.Trim();
        return Result.Success();
    }

    public Result MarkReimbursed()
    {
        if (Status != ExpenseClaimStatus.Approved)
        {
            return Result.Failure(Error.Conflict("expense_claim.not_approved", "Only an approved claim can be marked reimbursed."));
        }

        Status = ExpenseClaimStatus.Reimbursed;
        return Result.Success();
    }
}
