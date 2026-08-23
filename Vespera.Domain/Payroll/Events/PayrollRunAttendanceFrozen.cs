using Vespera.Domain.Common;

namespace Vespera.Domain.Payroll.Events;

public sealed record PayrollRunAttendanceFrozen(
    PayrollRunId PayrollRunId, TenantId TenantId, bool WasOverridden, string? OverrideReason, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
