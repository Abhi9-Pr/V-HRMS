using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Domain.UnitTests.Workspace;

public class AnnouncementTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Publish_Should_Set_IsPublished()
    {
        var announcement = CreateAnnouncement();

        var result = announcement.Publish(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        announcement.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Publish_Should_Fail_When_Already_Published()
    {
        var announcement = CreateAnnouncement();
        announcement.Publish(Now, "hr@vespera.test");

        var result = announcement.Publish(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Reject_An_Expiry_Before_The_Publish_Date()
    {
        var result = Announcement.Create(
            TenantId.New(), "Holiday notice", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
            Now,Now.AddDays(-1), Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Pin_Should_Set_IsPinned()
    {
        var announcement = CreateAnnouncement();

        var result = announcement.Pin(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        announcement.IsPinned.Should().BeTrue();
    }

    [Fact]
    public void Pin_Should_Fail_When_Already_Pinned()
    {
        var announcement = CreateAnnouncement();
        announcement.Pin(Now, "hr@vespera.test");

        var result = announcement.Pin(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Unpin_Should_Clear_IsPinned()
    {
        var announcement = CreateAnnouncement();
        announcement.Pin(Now, "hr@vespera.test");

        var result = announcement.Unpin(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        announcement.IsPinned.Should().BeFalse();
    }

    [Fact]
    public void Unpin_Should_Fail_When_Not_Pinned()
    {
        var announcement = CreateAnnouncement();

        var result = announcement.Unpin(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    private static Announcement CreateAnnouncement() =>
        Announcement.Create(
            TenantId.New(), "Holiday notice", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
            Now,null, Now, "hr@vespera.test").Value;
}
