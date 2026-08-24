using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class AddTicketCommentCommandHandler : IRequestHandler<AddTicketCommentCommand, Result>
{
    private readonly IWriteRepository<Ticket> _tickets;
    private readonly IReadRepository<Ticket> _ticketReads;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public AddTicketCommentCommandHandler(
        IWriteRepository<Ticket> tickets, IReadRepository<Ticket> ticketReads, IDateTimeProvider dateTimeProvider,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _tickets = tickets;
        _ticketReads = ticketReads;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(AddTicketCommentCommand request, CancellationToken cancellationToken)
    {
        var currentEmployeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(Error.Validation("ticket.no_employee", "The signed-in account is not linked to an employee."));
        }

        var ticket = await _ticketReads.FirstOrDefaultAsync(new TicketByIdSpecification(new TicketId(request.TicketId)), cancellationToken);
        if (ticket is null)
        {
            return Result.Failure(Error.NotFound("ticket.not_found", "Ticket not found."));
        }

        var parentCommentId = request.ParentCommentId is { } id ? new TicketCommentId(id) : (TicketCommentId?)null;

        var result = ticket.AddComment(
            currentEmployeeId.Value, request.Body, request.IsInternal, _dateTimeProvider.UtcNow, parentCommentId, request.AttachmentReferences);

        if (result.IsFailure)
        {
            return result;
        }

        _tickets.Update(ticket);
        return Result.Success();
    }
}
