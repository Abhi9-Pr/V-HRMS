using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Rosters;

public sealed record GenerateRosterCommand(
    Guid RotationPatternId,
    IReadOnlyList<Guid> EmployeeIds,
    DateOnly RangeStart,
    DateOnly RangeEnd,
    DateOnly PatternAnchorDate) : IRequest<Result<int>>;
