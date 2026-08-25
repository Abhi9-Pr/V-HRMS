using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

public sealed record WriteOffAssetCommand(
    Guid RecoveryId, decimal Amount, Currency Currency, string Reason, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
