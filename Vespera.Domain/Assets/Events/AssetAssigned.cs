using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Assets.Events;

public sealed record AssetAssigned(AssetAssignmentId AssetAssignmentId, AssetId AssetId, EmployeeId EmployeeId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
