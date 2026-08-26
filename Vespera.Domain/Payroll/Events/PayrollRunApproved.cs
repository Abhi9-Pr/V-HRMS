using Vespera.Domain.Common;

namespace Vespera.Domain.Payroll.Events;

public sealed record PayrollRunApproved(PayrollRunId PayrollRunId, TenantId TenantId, string ApprovedBy, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
