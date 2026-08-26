using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class RateTicketSatisfactionCommandHandler : IRequestHandler<RateTicketSatisfactionCommand, Result>
{
    private readonly IReadRepository<Ticket> _tickets;

    public RateTicketSatisfactionCommandHandler(IReadRepository<Ticket> tickets)
    {
        _tickets = tickets;
    }

    public async Task<Result> Handle(RateTicketSatisfactionCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.FirstOrDefaultAsync(new TicketByIdSpecification(new TicketId(request.TicketId)), cancellationToken);
        if (ticket is null)
        {
            return Result.Failure(Error.NotFound("ticket.not_found", "Ticket not found."));
        }

        return ticket.RateSatisfaction(request.Rating);
    }
}
