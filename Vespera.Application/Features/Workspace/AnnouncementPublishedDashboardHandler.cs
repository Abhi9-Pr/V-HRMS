using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Domain.Workspace;
using Vespera.Domain.Workspace.Events;

namespace Vespera.Application.Features.Workspace;

/// <summary>Pushes a freshly published announcement to every connected client in the tenant
/// (audience-scope narrowing happens client-side against the payload's own scope-free summary —
/// the dashboard re-fetches nothing, it just prepends this to the announcements widget's list).
/// Outbox-driven, no ambient tenant context, hence <see cref="IReadRepositoryAdmin{T}"/>.</summary>
public sealed class AnnouncementPublishedDashboardHandler : INotificationHandler<DomainEventNotification<AnnouncementPublished>>
{
    private readonly IReadRepositoryAdmin<Announcement> _announcementsAdmin;
    private readonly IDashboardRealtimePublisher _publisher;

    public AnnouncementPublishedDashboardHandler(IReadRepositoryAdmin<Announcement> announcementsAdmin, IDashboardRealtimePublisher publisher)
    {
        _announcementsAdmin = announcementsAdmin;
        _publisher = publisher;
    }

    public async Task Handle(DomainEventNotification<AnnouncementPublished> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var announcements = await _announcementsAdmin.ListIgnoringFiltersAsync(
            new AnnouncementByIdSpecification(domainEvent.TenantId, domainEvent.AnnouncementId), cancellationToken);
        var announcement = announcements.Count > 0 ? announcements[0] : null;
        if (announcement is null)
        {
            return;
        }

        await _publisher.PublishAnnouncementAsync(
            domainEvent.TenantId.Value,
            new AnnouncementPublishedPayload(
                announcement.Id.Value, announcement.Title, announcement.Priority.ToString(), announcement.IsPinned, announcement.PublishAt),
            cancellationToken);
    }
}
