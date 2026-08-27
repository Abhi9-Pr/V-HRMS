using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Dashboard.Widgets;

/// <summary>Every roster row for one employee — deliberately unfiltered by date/status, since
/// <see cref="RosterAssignmentResolver.Resolve"/> needs the override/base distinction and
/// publish status to pick the right one for a given day, and that logic isn't
/// SQL-translatable. Bounded by one employee's roster history, so loading it whole is cheap.</summary>
public sealed class ShiftRostersByEmployeeSpecification : ISpecification<ShiftRoster>
{
    public ShiftRostersByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = row => row.TenantId == tenantId && row.EmployeeId == employeeId;
    }

    public Expression<Func<ShiftRoster, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<ShiftRoster, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ShiftRoster, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
