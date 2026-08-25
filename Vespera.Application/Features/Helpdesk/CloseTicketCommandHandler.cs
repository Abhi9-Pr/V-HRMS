using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class CloseTicketCommandHandler : IRequestHandler<CloseTicketCommand, Result>
{
    private readonly IReadRepository<Ticket> _tickets;

    public CloseTicketCommandHandler(IReadRepository<Ticket> tickets)
    {
        _tickets = tickets;
    }

    public async Task<Result> Handle(CloseTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.FirstOrDefaultAsync(new TicketByIdSpecification(new TicketId(request.TicketId)), cancellationToken);
        if (ticket is null)
        {
            return Result.Failure(Error.NotFound("ticket.not_found", "Ticket not found."));
        }

        return ticket.Close();
    }
}
