using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record SubmitInterviewScorecardCommand(
    Guid InterviewId, Guid InterviewerId, int Rating, string? Notes, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
