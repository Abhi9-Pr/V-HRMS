using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record UploadTicketAttachmentCommand(Guid TicketId, byte[] Content, string FileName, string? IdempotencyKey)
    : IRequest<Result<string>>, IIdempotentRequest;
