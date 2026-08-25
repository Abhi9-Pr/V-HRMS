using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

/// <summary>A cheap pre-check for <c>HolidaysController.List</c>'s conditional-GET support (see
/// docs/api-mobile-contract.md) — computed from just the matching holidays' ids/RowVersions,
/// without running the full paged/mapped list query.</summary>
public sealed record GetHolidaysETagQuery(Guid? LocationId) : IRequest<Result<string>>;
