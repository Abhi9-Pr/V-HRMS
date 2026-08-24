using Mapster;
using MediatR;
using Vespera.Domain.Common;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, Result<TicketDto>>
{
    private readonly IReadRepository<Ticket> _tickets;

    public GetTicketByIdQueryHandler(IReadRepository<Ticket> tickets)
    {
        _tickets = tickets;
    }

    public async Task<Result<TicketDto>> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.FirstOrDefaultAsync(new TicketByIdSpecification(new TicketId(request.Id)), cancellationToken);
        if (ticket is null)
        {
            return Result.Failure<TicketDto>(Error.NotFound("ticket.not_found", "Ticket not found."));
        }

        return Result.Success(ticket.Adapt<TicketDto>());
    }
}
