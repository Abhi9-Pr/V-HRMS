using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses;

public sealed class GetExpenseClaimByIdQueryHandler : IRequestHandler<GetExpenseClaimByIdQuery, Result<ExpenseClaimDto>>
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetExpenseClaimByIdQueryHandler(IReadRepository<ExpenseClaim> expenseClaims, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _expenseClaims = expenseClaims;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<ExpenseClaimDto>> Handle(GetExpenseClaimByIdQuery request, CancellationToken cancellationToken)
    {
        var claim = await _expenseClaims.FirstOrDefaultAsync(new ExpenseClaimByIdSpecification(new ExpenseClaimId(request.Id)), cancellationToken);
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);

        if (claim is null || employeeId is null || claim.EmployeeId != employeeId.Value)
        {
            return Result.Failure<ExpenseClaimDto>(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        return Result.Success(claim.Adapt<ExpenseClaimDto>());
    }
}
