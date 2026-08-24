using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Expenses;

/// <summary>The 11a -> Phase 10 payroll integration point: pushes an approved claim's total into a
/// Draft payroll run as a reimbursement component and marks the claim Reimbursed.</summary>
public sealed class SettleExpenseClaimCommandHandler : IRequestHandler<SettleExpenseClaimCommand, Result>
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims;
    private readonly IReadRepository<PayrollRun> _payrollRuns;

    public SettleExpenseClaimCommandHandler(IReadRepository<ExpenseClaim> expenseClaims, IReadRepository<PayrollRun> payrollRuns)
    {
        _expenseClaims = expenseClaims;
        _payrollRuns = payrollRuns;
    }

    public async Task<Result> Handle(SettleExpenseClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _expenseClaims.FirstOrDefaultAsync(
            new ExpenseClaimByIdSpecification(new ExpenseClaimId(request.ExpenseClaimId)), cancellationToken);

        if (claim is null)
        {
            return Result.Failure(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        if (claim.Status != ExpenseClaimStatus.Approved)
        {
            return Result.Failure(Error.Conflict("expense_claim.not_approved", "Only an approved claim can be settled."));
        }

        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(
            new PayrollRunByIdSpecification(new PayrollRunId(request.PayrollRunId)), cancellationToken);

        if (payrollRun is null)
        {
            return Result.Failure(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        var amount = claim.Total(claim.SettlementCurrency);
        var addResult = payrollRun.AddReimbursement(claim.EmployeeId, amount, claim.Id.Value);
        if (addResult.IsFailure)
        {
            return addResult;
        }

        return claim.MarkReimbursed();
    }
}
