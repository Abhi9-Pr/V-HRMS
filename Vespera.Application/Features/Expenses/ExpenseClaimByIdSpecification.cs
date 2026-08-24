using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses;

public sealed class ExpenseClaimByIdSpecification : ISpecification<ExpenseClaim>
{
    public ExpenseClaimByIdSpecification(ExpenseClaimId claimId)
    {
        Criteria = claim => claim.Id == claimId;
    }

    public Expression<Func<ExpenseClaim, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ExpenseClaim, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ExpenseClaim, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
