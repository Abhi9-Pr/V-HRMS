using Vespera.Domain.Leave;

namespace Vespera.Domain.Services;

public sealed class LossOfPayCalculator
{
    public decimal CalculateLopDays(LeaveBalance balance, decimal requestedDays)
    {
        ArgumentNullException.ThrowIfNull(balance);

        if (requestedDays <= 0)
        {
            return 0m;
        }

        return Math.Max(0m, requestedDays - balance.Available);
    }
}
