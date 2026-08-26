using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record CompleteAssetRecoveryCommand(Guid RecoveryId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
