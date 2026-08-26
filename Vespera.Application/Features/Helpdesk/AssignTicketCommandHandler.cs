using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result>
{
    private readonly IReadRepository<Ticket> _tickets;

    public AssignTicketCommandHandler(IReadRepository<Ticket> tickets)
    {
        _tickets = tickets;
    }

    public async Task<Result> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.FirstOrDefaultAsync(new TicketByIdSpecification(new TicketId(request.TicketId)), cancellationToken);
        if (ticket is null)
        {
            return Result.Failure(Error.NotFound("ticket.not_found", "Ticket not found."));
        }

        return ticket.AssignTo(new EmployeeId(request.EmployeeId));
    }
}
