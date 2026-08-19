using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Leave.Events;

public sealed record LeaveRejected(
    LeaveRequestId LeaveRequestId, EmployeeId EmployeeId, EmployeeId ApproverId, string Reason, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
