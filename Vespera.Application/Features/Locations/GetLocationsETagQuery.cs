using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Locations;

/// <summary>A cheap pre-check for <c>LocationsController.List</c>'s conditional-GET support (see
/// docs/api-mobile-contract.md) — computed from just the matching locations' ids/RowVersions,
/// without running the full paged/mapped list query.</summary>
public sealed record GetLocationsETagQuery : IRequest<Result<string>>;
