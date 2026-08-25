using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record AddPipelineStageCommand(Guid RequisitionId, string StageName, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
