using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class ExpensePoliciesPagedSpecification : ISpecification<ExpensePolicy>
{
    public ExpensePoliciesPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = policy => policy.TenantId == tenantId && !policy.IsDeleted;
        OrderBy = [(policy => (object)policy.Category, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<ExpensePolicy, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ExpensePolicy, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ExpensePolicy, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
