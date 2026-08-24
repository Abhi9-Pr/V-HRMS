using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class TicketCategoryByIdSpecification : ISpecification<TicketCategory>
{
    public TicketCategoryByIdSpecification(TicketCategoryId categoryId)
    {
        Criteria = category => category.Id == categoryId;
    }

    public Expression<Func<TicketCategory, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<TicketCategory, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<TicketCategory, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
