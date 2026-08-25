using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record RecordAssetConditionCommand(
    Guid AssignmentId, AssetConditionRating Rating, string? Notes, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
