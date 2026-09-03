using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Payroll;

/// <summary>Batch counterpart to <see cref="ApprovedLopLeaveRequestsOverlappingPeriodSpecification"/>
/// — one query for every employee a payroll dry-run needs, instead of one per employee. See
/// <see cref="RunDryRunCommandHandler"/>.</summary>
public sealed class ApprovedLopLeaveRequestsOverlappingPeriodForEmployeesSpecification : ISpecification<LeaveRequest>
{
    public ApprovedLopLeaveRequestsOverlappingPeriodForEmployeesSpecification(
        TenantId tenantId, IReadOnlyCollection<EmployeeId> employeeIds, DateOnly periodStart, DateOnly periodEnd)
    {
        Criteria = request =>
            request.TenantId == tenantId && employeeIds.Contains(request.EmployeeId) &&
            request.Status == LeaveRequestStatus.Approved && request.LossOfPayDays > 0 &&
            request.Period.Start <= periodEnd && request.Period.End >= periodStart;
    }

    public Expression<Func<LeaveRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
