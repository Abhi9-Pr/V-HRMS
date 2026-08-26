using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record ReturnAssetCommand(Guid AssignmentId, string Condition, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
