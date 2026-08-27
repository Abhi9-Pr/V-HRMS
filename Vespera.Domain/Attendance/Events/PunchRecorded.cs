using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Attendance.Events;

public sealed record PunchRecorded(TenantId TenantId, EmployeeId EmployeeId, DateTimeOffset PunchedAtUtc, PunchType PunchType, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
