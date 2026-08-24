using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record RecordDamageAssessmentCommand(Guid RecoveryId, string Notes, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
