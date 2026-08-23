using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Attendance.Events;

public sealed record RegularizationRejected(RegularizationRequestId RequestId, EmployeeId EmployeeId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
