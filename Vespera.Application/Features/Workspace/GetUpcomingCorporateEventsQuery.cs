using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record GetUpcomingCorporateEventsQuery : IRequest<Result<IReadOnlyList<CorporateEventSummaryDto>>>;

public sealed record CorporateEventSummaryDto(
    Guid Id, string Title, string Description, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string LocationText, string? MyResponse);
