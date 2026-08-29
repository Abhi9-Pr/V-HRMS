using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary>A cheap pre-check for <c>LeaveTypesController.ListLeaveTypes</c>'s conditional-GET
/// support (see docs/api-mobile-contract.md) — computed from just the tenant's leave types'
/// ids/RowVersions, without running the full mapped list query.</summary>
public sealed record GetLeaveTypesETagQuery : IRequest<Result<string>>;
