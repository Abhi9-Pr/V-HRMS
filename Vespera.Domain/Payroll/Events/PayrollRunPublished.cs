using Vespera.Domain.Common;

namespace Vespera.Domain.Payroll.Events;

public sealed record PayrollRunPublished(PayrollRunId PayrollRunId, TenantId TenantId, int EmployeeCount, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
