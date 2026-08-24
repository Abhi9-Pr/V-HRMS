using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record CreateCandidateCommand(
    Guid JobRequisitionId, string FullName, string Email, string Phone, string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
