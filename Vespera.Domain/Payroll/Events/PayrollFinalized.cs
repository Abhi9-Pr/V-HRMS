using Vespera.Domain.Common;

namespace Vespera.Domain.Payroll.Events;

public sealed record PayrollFinalized(PayrollRunId PayrollRunId, int Month, int Year, int EmployeeCount, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
