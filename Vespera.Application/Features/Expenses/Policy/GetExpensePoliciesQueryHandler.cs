using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class GetExpensePoliciesQueryHandler : IRequestHandler<GetExpensePoliciesQuery, Result<PagedResult<ExpensePolicyDto>>>
{
    private readonly IReadRepository<ExpensePolicy> _expensePolicies;
    private readonly ITenantContext _tenantContext;

    public GetExpensePoliciesQueryHandler(IReadRepository<ExpensePolicy> expensePolicies, ITenantContext tenantContext)
    {
        _expensePolicies = expensePolicies;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<ExpensePolicyDto>>> Handle(GetExpensePoliciesQuery request, CancellationToken cancellationToken)
    {
        var specification = new ExpensePoliciesPagedSpecification(_tenantContext.TenantId, request.Paging);

        var policies = await _expensePolicies.ListAsync(specification, cancellationToken);
        var totalCount = await _expensePolicies.CountAsync(specification, cancellationToken);

        var items = policies.Adapt<List<ExpensePolicyDto>>();

        return Result.Success(new PagedResult<ExpensePolicyDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
