using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record RecordAssetReceivedCommand(Guid RecoveryId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
