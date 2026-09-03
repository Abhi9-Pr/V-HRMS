using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Payroll;

/// <summary>Batch counterpart to <see cref="ApprovedLopLeaveRequestsOverlappingPeriodSpecification"/>
/// — one query for every employee a payroll dry-run needs, instead of one per employee. See
/// <see cref="RunDryRunCommandHandler"/>.
///
/// Does NOT filter on period overlap here — see the same note on
/// <see cref="ApprovedLopLeaveRequestsOverlappingPeriodSpecification"/> for why
/// <c>Period.Start</c>/<c>Period.End</c> can't appear in a translatable query. Callers must apply
/// the period-overlap check in memory on the result.</summary>
public sealed class ApprovedLopLeaveRequestsOverlappingPeriodForEmployeesSpecification : ISpecification<LeaveRequest>
{
    public ApprovedLopLeaveRequestsOverlappingPeriodForEmployeesSpecification(
        TenantId tenantId, IReadOnlyCollection<EmployeeId> employeeIds)
    {
        Criteria = request =>
            request.TenantId == tenantId && employeeIds.Contains(request.EmployeeId) &&
            request.Status == LeaveRequestStatus.Approved && request.LossOfPayDays > 0;
    }

    public Expression<Func<LeaveRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<LeaveRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<LeaveRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
