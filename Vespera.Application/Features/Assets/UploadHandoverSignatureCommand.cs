using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record UploadHandoverSignatureCommand(
    Guid AssignmentId, byte[] Content, string FileName, string? IdempotencyKey) : IRequest<Result<string>>, IIdempotentRequest;
