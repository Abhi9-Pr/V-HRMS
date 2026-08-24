using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed record GetTicketsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<TicketSummaryDto>>>;

public sealed record TicketSummaryDto(
    Guid Id, string Subject, TicketPriority Priority, TicketStatus Status, Guid CategoryId, Guid? AssignedTo,
    DateTimeOffset RaisedAt, DateTimeOffset DueAt);
