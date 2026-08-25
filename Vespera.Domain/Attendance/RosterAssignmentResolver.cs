using Vespera.Domain.Eis;

namespace Vespera.Domain.Attendance;

/// <summary>
/// Resolves which shift (if any) an employee is assigned to on a given date, from an
/// already-loaded set of <see cref="ShiftRoster"/> rows. Pure — no I/O, no repository access;
/// callers (roster queries, punch validation, attendance-day computation) load the candidate rows
/// themselves and pass them in.
///
/// Precedence: a <see cref="ShiftRosterStatus.Published"/> row with <see cref="ShiftRoster.IsOverride"/>
/// covering the date wins over a <see cref="ShiftRosterStatus.Published"/> non-override row covering
/// the date; a <see cref="ShiftRosterStatus.Draft"/> row never resolves regardless of tier. If more
/// than one row of the same tier covers the same date (not expected in normal operation, since
/// <c>GenerateRosterCommand</c> never produces overlapping runs for one employee), the most recently
/// created row (<see cref="ShiftRoster.CreatedAt"/>) wins, as a deterministic tie-break rather than an
/// arbitrary one.
/// </summary>
public static class RosterAssignmentResolver
{
    public static ShiftId? Resolve(IReadOnlyList<ShiftRoster> rosterRows, EmployeeId employeeId, DateOnly date)
    {
        var candidates = rosterRows.Where(row =>
            row.EmployeeId == employeeId &&
            row.Status == ShiftRosterStatus.Published &&
            row.Period.Contains(date));

        var overrideMatch = candidates
            .Where(row => row.IsOverride)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefault();

        if (overrideMatch is not null)
        {
            return overrideMatch.ShiftId;
        }

        var baseMatch = candidates
            .Where(row => !row.IsOverride)
            .OrderByDescending(row => row.CreatedAt)
            .FirstOrDefault();

        return baseMatch?.ShiftId;
    }
}
