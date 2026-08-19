using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Leave.Events;

public sealed record LeaveApproved(
    LeaveRequestId LeaveRequestId, EmployeeId EmployeeId, DateRange Period, EmployeeId ApproverId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
