using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed record GetShiftsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<ShiftDto>>>;

public sealed record ShiftDto(Guid Id, string Name, TimeOnly StartTime, TimeOnly EndTime, int GraceMinutes, int BreakMinutes, bool IsOvernight);

/// <summary>Compact profile for mobile list responses — ID plus display-critical fields only.</summary>
public sealed record ShiftSummaryDto(Guid Id, string Name);
