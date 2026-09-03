using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;
using Vespera.Domain.Workspace.Events;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class AnnouncementPublishedDashboardHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepositoryAdmin<Announcement> _announcementsAdmin = Substitute.For<IReadRepositoryAdmin<Announcement>>();
    private readonly IDashboardRealtimePublisher _publisher = Substitute.For<IDashboardRealtimePublisher>();

    private AnnouncementPublishedDashboardHandler CreateHandler() => new(_announcementsAdmin, _publisher);

    private static Announcement CreateAnnouncement()
    {
        var announcement = Announcement.Create(
            TenantId, "Holiday notice", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
            Now, null, Now, "hr@vespera.test").Value;
        announcement.Publish(Now, "hr@vespera.test");
        return announcement;
    }

    [Fact]
    public async Task Handle_Should_Publish_The_Announcement_Payload_To_The_Tenant()
    {
        var announcement = CreateAnnouncement();
        _announcementsAdmin.ListIgnoringFiltersAsync(Arg.Any<AnnouncementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Announcement>)[announcement]);

        var notification = new DomainEventNotification<AnnouncementPublished>(new AnnouncementPublished(announcement.Id, TenantId, Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _publisher.Received(1).PublishAnnouncementAsync(
            TenantId.Value,
            Arg.Is<AnnouncementPublishedPayload>(p => p.AnnouncementId == announcement.Id.Value && p.Title == announcement.Title),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Announcement_No_Longer_Exists()
    {
        _announcementsAdmin.ListIgnoringFiltersAsync(Arg.Any<AnnouncementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Announcement>)[]);

        var notification = new DomainEventNotification<AnnouncementPublished>(
            new AnnouncementPublished(new AnnouncementId(Guid.NewGuid()), TenantId, Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _publisher.DidNotReceive().PublishAnnouncementAsync(
            Arg.Any<Guid>(), Arg.Any<AnnouncementPublishedPayload>(), Arg.Any<CancellationToken>());
    }
}
