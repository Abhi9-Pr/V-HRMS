using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record RescheduleInterviewCommand(
    Guid InterviewId, DateTimeOffset NewScheduledAt, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
