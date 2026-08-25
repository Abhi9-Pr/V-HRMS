using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Attendance.Events;

public sealed record RegularizationApproved(
    RegularizationRequestId RequestId, TenantId TenantId, EmployeeId EmployeeId, AttendanceDayId AttendanceDayId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
