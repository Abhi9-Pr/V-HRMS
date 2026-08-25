using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Rosters;

/// <summary>Every roster row for the given employees matching <paramref name="status"/> — used by
/// both <c>PublishRosterCommandHandler</c> (Draft) and <c>GetRosterQueryHandler</c> (Published).
/// Deliberately does NOT filter by date range here: <see cref="ShiftRoster.Period"/> is persisted
/// as a single opaque converted column (see <c>ShiftRosterConfiguration</c> — <see cref="Domain.ValueObjects.DateRange"/>
/// has no settable properties for EF to populate an owned type post-construction, same reasoning
/// as <c>GeoCoordinate</c>), so a server-side overlap predicate isn't translatable; callers filter
/// the returned rows by <c>Period.Overlaps(...)</c> in memory after this fetch — the same
/// client-side-filter-after-one-fetch pattern already used by the retention/offboarding sweeps for
/// an equivalent translation limitation.</summary>
public sealed class RostersByEmployeesAndStatusSpecification : ISpecification<ShiftRoster>
{
    public RostersByEmployeesAndStatusSpecification(TenantId tenantId, IReadOnlyList<Guid> employeeIds, ShiftRosterStatus status)
    {
        // Compares the whole converted EmployeeId struct, not its .Value member -- see
        // RosterEmployeesSpecification's comment for why ".Value" member access on a
        // value-converted typed-id property doesn't translate.
        var ids = employeeIds.Select(id => new EmployeeId(id)).ToList();
        Criteria = roster =>
            roster.TenantId == tenantId &&
            ids.Contains(roster.EmployeeId) &&
            roster.Status == status;
    }

    public Expression<Func<ShiftRoster, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<ShiftRoster, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ShiftRoster, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
