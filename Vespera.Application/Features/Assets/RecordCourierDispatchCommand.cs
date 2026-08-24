using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record RecordCourierDispatchCommand(
    Guid RecoveryId, string Carrier, string TrackingReference, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
