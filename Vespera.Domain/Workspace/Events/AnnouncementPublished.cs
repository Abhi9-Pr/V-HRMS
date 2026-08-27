using Vespera.Domain.Common;

namespace Vespera.Domain.Workspace.Events;

public sealed record AnnouncementPublished(AnnouncementId AnnouncementId, TenantId TenantId, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
