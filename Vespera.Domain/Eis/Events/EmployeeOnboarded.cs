using Vespera.Domain.Common;

namespace Vespera.Domain.Eis.Events;

public sealed record EmployeeOnboarded(EmployeeId EmployeeId, TenantId TenantId, DateOnly DateOfJoining, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
