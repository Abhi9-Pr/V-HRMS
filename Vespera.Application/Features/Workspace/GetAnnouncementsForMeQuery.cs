using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record GetAnnouncementsForMeQuery : IRequest<Result<IReadOnlyList<AnnouncementSummaryDto>>>;

public sealed record AnnouncementSummaryDto(
    Guid Id, string Title, string Body, string Priority, bool IsPinned, DateTimeOffset PublishAt, bool IsAcknowledged);
