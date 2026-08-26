using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Leave.Events;

public sealed record LeaveEncashed(
    LeaveBalanceId LeaveBalanceId, TenantId TenantId, EmployeeId EmployeeId, LeaveTypeId LeaveTypeId, decimal Days,
    DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
