using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record CompleteInterviewCommand(
    Guid InterviewId, string Feedback, int Rating, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
