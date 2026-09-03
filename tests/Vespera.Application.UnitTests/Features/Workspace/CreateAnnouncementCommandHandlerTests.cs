using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class CreateAnnouncementCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishAt = Now;

    private readonly IWriteRepository<Announcement> _announcements = Substitute.For<IWriteRepository<Announcement>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateAnnouncementCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns((Guid?)UserId);
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private CreateAnnouncementCommandHandler CreateHandler() => new(_announcements, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Create_And_Add_The_Announcement()
    {
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _announcements.Received(1).AddAsync(
            Arg.Is<Announcement>(a => a.Title == "Holiday notice" && !a.IsPublished), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Publish_Immediately_When_Requested()
    {
        var result = await CreateHandler().Handle(CreateCommand() with { PublishImmediately = true }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _announcements.Received(1).AddAsync(Arg.Is<Announcement>(a => a.IsPublished), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Announcement_Is_Invalid()
    {
        var result = await CreateHandler().Handle(CreateCommand() with { ExpiresAt = PublishAt.AddDays(-1) }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _announcements.DidNotReceive().AddAsync(Arg.Any<Announcement>(), Arg.Any<CancellationToken>());
    }

    private static CreateAnnouncementCommand CreateCommand() => new(
        "Holiday notice", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
        PublishAt, null, false, null);
}
