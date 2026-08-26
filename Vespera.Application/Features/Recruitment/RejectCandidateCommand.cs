using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record RejectCandidateCommand(Guid CandidateId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
