using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record PublishRequisitionCommand(Guid RequisitionId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
