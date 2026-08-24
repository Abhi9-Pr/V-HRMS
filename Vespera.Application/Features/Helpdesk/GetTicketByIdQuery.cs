using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed record GetTicketByIdQuery(Guid Id) : IRequest<Result<TicketDto>>;

public sealed record TicketDto(
    Guid Id,
    string Subject,
    string Description,
    TicketPriority Priority,
    TicketStatus Status,
    Guid CategoryId,
    Guid RaisedBy,
    Guid? AssignedTo,
    DateTimeOffset RaisedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? ResolvedAt,
    bool SlaBreachNotified,
    bool SlaWarningNotified,
    int? SatisfactionRating,
    IReadOnlyList<TicketCommentDto> Comments);

/// <summary>Flat, not pre-nested — <see cref="ParentCommentId"/> is exposed so the client threads
/// replies itself (a small admin-facing list, not worth a recursive server-side DTO shape).</summary>
public sealed record TicketCommentDto(
    Guid Id, Guid AuthorId, string Body, bool IsInternal, DateTimeOffset CreatedAt, Guid? ParentCommentId,
    IReadOnlyList<string> AttachmentReferences);
