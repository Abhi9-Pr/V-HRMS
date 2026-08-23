using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed record GetRotationPatternsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<RotationPatternDto>>>;

public sealed record RotationPatternDayDto(int SequenceNumber, Guid? ShiftId);

public sealed record RotationPatternDto(Guid Id, string Name, IReadOnlyList<RotationPatternDayDto> Days);

/// <summary>Compact profile for mobile list responses — ID plus display-critical fields only.</summary>
public sealed record RotationPatternSummaryDto(Guid Id, string Name);
