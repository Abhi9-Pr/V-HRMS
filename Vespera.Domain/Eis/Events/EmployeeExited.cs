using Vespera.Domain.Common;

namespace Vespera.Domain.Eis.Events;

public sealed record EmployeeExited(EmployeeId EmployeeId, TenantId TenantId, DateOnly ExitDate, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
