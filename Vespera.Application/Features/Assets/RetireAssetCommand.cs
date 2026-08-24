using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record RetireAssetCommand(Guid AssetId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
