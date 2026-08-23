using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed record CreateShiftCommand(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int GraceMinutes,
    int BreakMinutes,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
