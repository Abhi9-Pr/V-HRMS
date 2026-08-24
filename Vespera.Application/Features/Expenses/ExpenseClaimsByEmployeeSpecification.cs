using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses;

public sealed class ExpenseClaimsByEmployeeSpecification : ISpecification<ExpenseClaim>
{
    public ExpenseClaimsByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId, PagedRequest paging)
    {
        Criteria = claim => claim.TenantId == tenantId && claim.EmployeeId == employeeId;
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<ExpenseClaim, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ExpenseClaim, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ExpenseClaim, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging { get; }
}
