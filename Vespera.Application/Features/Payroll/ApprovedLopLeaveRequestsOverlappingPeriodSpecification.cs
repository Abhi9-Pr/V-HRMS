using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Payroll;

/// <summary>Approved leave requests flagged with Loss-of-Pay days for one employee — reuses the
/// Leave module's own <c>LeaveRequest.LossOfPayDays</c> directly rather than a separate
/// Attendance/Payroll LOP record.
///
/// Does NOT filter on period overlap here: <c>LeaveRequestConfiguration</c> maps
/// <c>Period</c> (a <see cref="Vespera.Domain.ValueObjects.DateRange"/>) via a single-column
/// <c>HasConversion</c>, not <c>OwnsOne</c> — see that configuration's own comment for why. EF
/// Core can't translate member access (<c>Period.Start</c>/<c>Period.End</c>) through a
/// whole-property conversion, so a query with those predicates throws
/// <c>InvalidOperationException</c> at runtime instead of executing — this was never caught by
/// unit tests because they mock the repository. Callers must filter the (small, already
/// tenant/employee/status/LOP-scoped) result on period overlap in memory instead.</summary>
public sealed class ApprovedLopLeaveRequestsOverlappingPeriodSpecification : ISpecification<LeaveRequest>
{
    public ApprovedLopLeaveRequestsOverlappingPeriodSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = request =>
            request.TenantId == tenantId && request.EmployeeId == employeeId &&
            request.Status == LeaveRequestStatus.Approved && request.LossOfPayDays > 0;
    }

    public Expression<Func<LeaveRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
