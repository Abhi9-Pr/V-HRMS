using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Domain.Services;

/// <summary>Computes how many days a single employee accrues for a single <see cref="LeavePolicy"/>
/// over a period — pro-rated for a mid-period <see cref="Employee.DateOfJoining"/> or
/// <see cref="Employee.ExitDate"/>, and picking the applicable <see cref="TenureAccrualTier"/> (if
/// any) for the employee's tenure as of the period end. A pure calculator: callers (the accrual
/// hosted service) decide whether the employee even qualifies to run it (tenure gate) and are
/// responsible for posting the result to a <see cref="LeaveBalance"/>.</summary>
public sealed class LeaveAccrualCalculator
{
    /// <summary>True once the employee has served <see cref="LeavePolicy.MinimumTenureMonthsForAccrual"/>
    /// as of <paramref name="asOf"/>.</summary>
    public bool IsEligibleForAccrual(LeavePolicy policy, Employee employee, DateOnly asOf) =>
        TenureInMonths(employee.DateOfJoining, asOf) >= policy.MinimumTenureMonthsForAccrual;

    /// <summary>The days a monthly-frequency policy accrues for the given calendar month,
    /// pro-rated for a mid-month join or exit.</summary>
    public decimal CalculateMonthlyAccrual(LeavePolicy policy, Employee employee, DateOnly monthStart, DateOnly monthEnd)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(employee);

        if (monthEnd < monthStart || !IsEligibleForAccrual(policy, employee, monthEnd))
        {
            return 0m;
        }

        var effectiveStart = Max(monthStart, employee.DateOfJoining);
        var effectiveEnd = Min(monthEnd, employee.ExitDate ?? monthEnd);
        if (effectiveStart > effectiveEnd)
        {
            return 0m;
        }

        var daysInMonth = monthEnd.DayNumber - monthStart.DayNumber + 1;
        var workedDays = effectiveEnd.DayNumber - effectiveStart.DayNumber + 1;
        var monthlyRate = ResolveMonthlyRate(policy, TenureInMonths(employee.DateOfJoining, monthEnd));

        return decimal.Round(monthlyRate * workedDays / daysInMonth, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>The days an annual-frequency policy grants for the given calendar year, pro-rated
    /// for a mid-year join (an exit part-way through the year does not claw back an already-granted
    /// annual lump sum — that is a policy call for a later phase, not this calculator's job).</summary>
    public decimal CalculateAnnualAccrual(LeavePolicy policy, Employee employee, int year)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(employee);

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);
        if (!IsEligibleForAccrual(policy, employee, yearEnd) || employee.DateOfJoining > yearEnd)
        {
            return 0m;
        }

        var effectiveStart = Max(yearStart, employee.DateOfJoining);
        var daysInYear = yearEnd.DayNumber - yearStart.DayNumber + 1;
        var eligibleDays = yearEnd.DayNumber - effectiveStart.DayNumber + 1;

        return decimal.Round(policy.AnnualEntitlementDays * eligibleDays / daysInYear, 4, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveMonthlyRate(LeavePolicy policy, int tenureMonths)
    {
        if (policy.TenureAccrualTiers.Count == 0)
        {
            return policy.AccrualRatePerMonth;
        }

        var tier = policy.TenureAccrualTiers
            .Where(t => t.MinimumTenureMonths <= tenureMonths)
            .OrderByDescending(t => t.MinimumTenureMonths)
            .Cast<TenureAccrualTier?>()
            .FirstOrDefault();

        return tier?.MonthlyRate ?? policy.AccrualRatePerMonth;
    }

    private static int TenureInMonths(DateOnly dateOfJoining, DateOnly asOf)
    {
        if (asOf < dateOfJoining)
        {
            return 0;
        }

        var months = ((asOf.Year - dateOfJoining.Year) * 12) + asOf.Month - dateOfJoining.Month;
        if (asOf.Day < dateOfJoining.Day)
        {
            months--;
        }

        return Math.Max(0, months);
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
}
