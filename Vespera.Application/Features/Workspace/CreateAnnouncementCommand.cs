using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed record CreateAnnouncementCommand(
    string Title,
    string Body,
    AnnouncementAudienceScope AudienceScope,
    Guid? TargetDepartmentId,
    Guid? TargetLocationId,
    AnnouncementPriority Priority,
    DateTimeOffset PublishAt,
    DateTimeOffset? ExpiresAt,
    bool PublishImmediately,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
