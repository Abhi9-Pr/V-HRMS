using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

/// <summary>Optional <paramref name="LocationId"/> filter — holidays are meaningfully queried
/// per-location, since there is no cross-location "applies everywhere" flag.</summary>
public sealed record GetHolidaysQuery(PagedRequest Paging, Guid? LocationId) : IRequest<Result<PagedResult<HolidayDto>>>;

public sealed record HolidayDto(Guid Id, Guid LocationId, DateOnly Date, string Name);

/// <summary>Compact profile for mobile list responses — ID plus display-critical fields only.</summary>
public sealed record HolidaySummaryDto(Guid Id, string Name);
