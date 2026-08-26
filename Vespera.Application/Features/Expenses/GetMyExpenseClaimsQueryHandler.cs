using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses;

public sealed class GetMyExpenseClaimsQueryHandler : IRequestHandler<GetMyExpenseClaimsQuery, Result<PagedResult<ExpenseClaimDto>>>
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims;
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetMyExpenseClaimsQueryHandler(
        IReadRepository<ExpenseClaim> expenseClaims, ITenantContext tenantContext, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _expenseClaims = expenseClaims;
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<PagedResult<ExpenseClaimDto>>> Handle(GetMyExpenseClaimsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Failure<PagedResult<ExpenseClaimDto>>(
                Error.Validation("expense_claim.no_employee", "The signed-in account is not linked to an employee."));
        }

        var specification = new ExpenseClaimsByEmployeeSpecification(_tenantContext.TenantId, employeeId.Value, request.Paging);

        var claims = await _expenseClaims.ListAsync(specification, cancellationToken);
        var totalCount = await _expenseClaims.CountAsync(specification, cancellationToken);

        var items = claims.Adapt<List<ExpenseClaimDto>>();

        return Result.Success(new PagedResult<ExpenseClaimDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
