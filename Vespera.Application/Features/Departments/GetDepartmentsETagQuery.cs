using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Departments;

/// <summary>A cheap pre-check for <c>DepartmentsController.List</c>'s conditional-GET support (see
/// docs/api-mobile-contract.md) — computed from just the matching departments' ids/RowVersions,
/// without running the full paged/mapped list query.</summary>
public sealed record GetDepartmentsETagQuery : IRequest<Result<string>>;
