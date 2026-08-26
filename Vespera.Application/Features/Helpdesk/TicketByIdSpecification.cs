using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class TicketByIdSpecification : ISpecification<Ticket>
{
    public TicketByIdSpecification(TicketId ticketId)
    {
        Criteria = ticket => ticket.Id == ticketId;
    }

    public Expression<Func<Ticket, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Ticket, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Ticket, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
