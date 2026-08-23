using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Payroll;

/// <summary>Approved leave requests flagged with Loss-of-Pay days whose period overlaps the
/// payroll run's period — reuses the Leave module's own <c>LeaveRequest.LossOfPayDays</c> directly
/// rather than a separate Attendance/Payroll LOP record.</summary>
public sealed class ApprovedLopLeaveRequestsOverlappingPeriodSpecification : ISpecification<LeaveRequest>
{
    public ApprovedLopLeaveRequestsOverlappingPeriodSpecification(
        TenantId tenantId, EmployeeId employeeId, DateOnly periodStart, DateOnly periodEnd)
    {
        Criteria = request =>
            request.TenantId == tenantId && request.EmployeeId == employeeId &&
            request.Status == LeaveRequestStatus.Approved && request.LossOfPayDays > 0 &&
            request.Period.Start <= periodEnd && request.Period.End >= periodStart;
    }

    public Expression<Func<LeaveRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
