using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class ResolveTicketCommandHandler : IRequestHandler<ResolveTicketCommand, Result>
{
    private readonly IReadRepository<Ticket> _tickets;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ResolveTicketCommandHandler(IReadRepository<Ticket> tickets, IDateTimeProvider dateTimeProvider)
    {
        _tickets = tickets;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ResolveTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.FirstOrDefaultAsync(new TicketByIdSpecification(new TicketId(request.TicketId)), cancellationToken);
        if (ticket is null)
        {
            return Result.Failure(Error.NotFound("ticket.not_found", "Ticket not found."));
        }

        return ticket.Resolve(_dateTimeProvider.UtcNow);
    }
}
