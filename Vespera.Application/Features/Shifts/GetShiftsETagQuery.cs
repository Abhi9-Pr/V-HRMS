using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

/// <summary>A cheap pre-check for <c>ShiftsController.List</c>'s conditional-GET support (see
/// docs/api-mobile-contract.md) — computed from just the tenant's Shift ids/RowVersions, without
/// running the full paged/mapped list query, so a 304 short-circuits before that heavier work.</summary>
public sealed record GetShiftsETagQuery : IRequest<Result<string>>;
