using Vespera.Domain.Leave;

namespace Vespera.Domain.Services;

public sealed class LeaveAccrualCalculator
{
    public decimal AccrueForPeriod(LeavePolicy policy, DateOnly from, DateOnly to)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (to < from)
        {
            return 0m;
        }

        var months = ((to.Year - from.Year) * 12) + to.Month - from.Month + 1;
        return policy.AccrualRatePerMonth * months;
    }
}
