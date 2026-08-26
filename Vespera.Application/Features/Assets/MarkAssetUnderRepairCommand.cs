using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record MarkAssetUnderRepairCommand(Guid AssetId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
