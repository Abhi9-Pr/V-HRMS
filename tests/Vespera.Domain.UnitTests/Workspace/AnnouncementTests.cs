using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
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

    [Fact]
    public void Create_Should_Fail_When_Title_Is_Blank()
    {
        var result = Announcement.Create(
            TenantId.New(), "  ", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
            Now, null, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("announcement.title_required");
    }

    [Fact]
    public void Create_Should_Fail_When_Department_Scoped_Without_A_Target_Department()
    {
        var result = Announcement.Create(
            TenantId.New(), "Holiday notice", "Office closed", AnnouncementAudienceScope.Department, null, null, AnnouncementPriority.Normal,
            Now, null, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("announcement.department_required");
    }

    [Fact]
    public void Create_Should_Fail_When_Location_Scoped_Without_A_Target_Location()
    {
        var result = Announcement.Create(
            TenantId.New(), "Holiday notice", "Office closed", AnnouncementAudienceScope.Location, null, null, AnnouncementPriority.Normal,
            Now, null, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("announcement.location_required");
    }

    [Fact]
    public void Create_Should_Succeed_For_A_Department_Scoped_Announcement()
    {
        var departmentId = DepartmentId.New();

        var result = Announcement.Create(
            TenantId.New(), "Holiday notice", "Office closed", AnnouncementAudienceScope.Department, departmentId, null, AnnouncementPriority.Normal,
            Now, null, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        result.Value.TargetDepartmentId.Should().Be(departmentId);
        result.Value.TargetLocationId.Should().BeNull();
    }

    [Fact]
    public void Publish_Should_Raise_AnnouncementPublished()
    {
        var announcement = CreateAnnouncement();

        announcement.Publish(Now, "hr@vespera.test");

        announcement.DomainEvents.Should().ContainSingle(e => e is Vespera.Domain.Workspace.Events.AnnouncementPublished);
    }

    [Fact]
    public void Expire_Should_Fail_When_Not_Published()
    {
        var announcement = CreateAnnouncement();

        var result = announcement.Expire(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("announcement.not_published");
    }

    [Fact]
    public void Expire_Should_Set_ExpiresAt_Once_Published()
    {
        var announcement = CreateAnnouncement();
        announcement.Publish(Now, "hr@vespera.test");
        var expiredAt = Now.AddDays(30);

        var result = announcement.Expire(expiredAt, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        announcement.ExpiresAt.Should().Be(expiredAt);
    }

    private static Announcement CreateAnnouncement() =>
        Announcement.Create(
            TenantId.New(), "Holiday notice", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
            Now,null, Now, "hr@vespera.test").Value;
}
