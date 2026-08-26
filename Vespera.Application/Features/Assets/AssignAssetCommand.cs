using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record AssignAssetCommand(Guid AssetId, Guid EmployeeId, string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
