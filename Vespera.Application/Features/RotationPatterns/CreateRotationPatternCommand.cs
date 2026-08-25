using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed record RotationPatternDayRequest(int SequenceNumber, Guid? ShiftId);

public sealed record CreateRotationPatternCommand(
    string Name,
    IReadOnlyList<RotationPatternDayRequest> Days,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
