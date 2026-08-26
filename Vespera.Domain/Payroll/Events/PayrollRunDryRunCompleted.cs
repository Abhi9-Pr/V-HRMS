using Vespera.Domain.Common;

namespace Vespera.Domain.Payroll.Events;

public sealed record PayrollRunDryRunCompleted(
    PayrollRunId PayrollRunId, TenantId TenantId, int EmployeeCount, string ExecutedBy, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
