using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record CreateSlaPolicyCommand(
    string Name,
    int ResponseTimeHours,
    int ResolutionTimeHours,
    TimeOnly BusinessHoursStart,
    TimeOnly BusinessHoursEnd,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
