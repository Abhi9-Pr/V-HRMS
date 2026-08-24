using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses;

public sealed class ExpensePoliciesByCategoriesSpecification : ISpecification<ExpensePolicy>
{
    public ExpensePoliciesByCategoriesSpecification(TenantId tenantId, IReadOnlyList<string> categories)
    {
        Criteria = policy => policy.TenantId == tenantId && categories.Contains(policy.Category) && !policy.IsDeleted;
    }

    public Expression<Func<ExpensePolicy, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ExpensePolicy, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ExpensePolicy, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
