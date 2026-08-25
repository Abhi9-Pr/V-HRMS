using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Rosters;

public sealed record OverrideRosterAssignmentCommand(
    Guid EmployeeId,
    DateOnly Date,
    Guid ShiftId) : IRequest<Result<Guid>>;
