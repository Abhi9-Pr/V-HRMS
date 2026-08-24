using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record AddTicketCommentCommand(
    Guid TicketId,
    string Body,
    bool IsInternal,
    Guid? ParentCommentId,
    IReadOnlyList<string>? AttachmentReferences,
    string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
