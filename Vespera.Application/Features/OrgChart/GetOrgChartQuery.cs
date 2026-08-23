using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.OrgChart;

/// <summary>The reporting hierarchy as of a given date. <see cref="RootEmployeeId"/> renders just
/// that employee's subtree; omitted, every employee with no active manager as of
/// <see cref="AsOf"/> becomes a root (there may be more than one — an org chart is a forest, not
/// necessarily a single tree, until every department head shares one CEO).</summary>
public sealed record GetOrgChartQuery(DateOnly AsOf, Guid? RootEmployeeId) : IRequest<Result<OrgChartResult>>;

/// <summary><see cref="ETag"/> is a stable hash of every employee/relationship fact that fed the
/// tree — see <c>GetOrgChartQueryHandler.ComputeETag</c> — so it changes exactly when a repeat
/// request would render differently, and stays the same when it wouldn't.</summary>
public sealed record OrgChartResult(string ETag, IReadOnlyList<OrgChartNodeDto> Roots);

public sealed record OrgChartNodeDto(
    Guid EmployeeId,
    string Code,
    string FullName,
    string Status,
    string? DesignationTitle,
    string? DepartmentName,
    IReadOnlyList<OrgChartNodeDto> Children);
