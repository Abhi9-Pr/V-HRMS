using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Designations;

/// <summary>A cheap pre-check for <c>DesignationsController.List</c>'s conditional-GET support (see
/// docs/api-mobile-contract.md) — computed from just the matching designations' ids/RowVersions,
/// without running the full paged/mapped list query.</summary>
public sealed record GetDesignationsETagQuery : IRequest<Result<string>>;
