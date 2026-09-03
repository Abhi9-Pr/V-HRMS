using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class SetAnnouncementPinnedCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<Announcement> _announcements = Substitute.For<IReadRepository<Announcement>>();
    private readonly IWriteRepository<Announcement> _announcementWriter = Substitute.For<IWriteRepository<Announcement>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public SetAnnouncementPinnedCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private SetAnnouncementPinnedCommandHandler CreateHandler() =>
        new(_announcements, _announcementWriter, _tenantContext, _currentUser, _dateTimeProvider);

    private static Announcement CreateAnnouncement() => Announcement.Create(
        TenantId, "Holiday notice", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
        Now, null, Now, "hr@vespera.test").Value;

    [Fact]
    public async Task Handle_Should_Fail_When_The_Announcement_Does_Not_Exist()
    {
        _announcements.FirstOrDefaultAsync(Arg.Any<AnnouncementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((Announcement?)null);

        var result = await CreateHandler().Handle(new SetAnnouncementPinnedCommand(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("announcement.not_found");
    }

    [Fact]
    public async Task Handle_Should_Pin_The_Announcement()
    {
        var announcement = CreateAnnouncement();
        _announcements.FirstOrDefaultAsync(Arg.Any<AnnouncementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(announcement);

        var result = await CreateHandler().Handle(new SetAnnouncementPinnedCommand(announcement.Id.Value, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        announcement.IsPinned.Should().BeTrue();
        _announcementWriter.Received(1).Update(announcement);
    }

    [Fact]
    public async Task Handle_Should_Unpin_The_Announcement()
    {
        var announcement = CreateAnnouncement();
        announcement.Pin(Now, "hr@vespera.test");
        _announcements.FirstOrDefaultAsync(Arg.Any<AnnouncementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(announcement);

        var result = await CreateHandler().Handle(new SetAnnouncementPinnedCommand(announcement.Id.Value, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        announcement.IsPinned.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Already_Pinned()
    {
        var announcement = CreateAnnouncement();
        announcement.Pin(Now, "hr@vespera.test");
        _announcements.FirstOrDefaultAsync(Arg.Any<AnnouncementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(announcement);

        var result = await CreateHandler().Handle(new SetAnnouncementPinnedCommand(announcement.Id.Value, true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _announcementWriter.DidNotReceive().Update(Arg.Any<Announcement>());
    }
}
