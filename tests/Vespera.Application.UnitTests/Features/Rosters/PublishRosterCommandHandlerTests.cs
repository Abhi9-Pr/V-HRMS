using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Rosters;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Rosters;

public class PublishRosterCommandHandlerTests
{
    private readonly IReadRepository<ShiftRoster> _rosterReader = Substitute.For<IReadRepository<ShiftRoster>>();
    private readonly IWriteRepository<ShiftRoster> _rosterWriter = Substitute.For<IWriteRepository<ShiftRoster>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly ShiftId ShiftId = ShiftId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public PublishRosterCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private PublishRosterCommandHandler CreateHandler() =>
        new(_rosterReader, _rosterWriter, _users, _notificationDispatcher, _tenantContext, _currentUser, _dateTimeProvider);

    private static User CreateUser() =>
        User.Create(TenantId, EmailAddress.Create("employee@vespera.test").Value, EmployeeId, Now, "system");

    [Fact]
    public async Task Handle_Should_Publish_Draft_Rows_And_Notify_The_Affected_Employee_Once()
    {
        var period = DateRange.Create(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 7)).Value;
        var roster = ShiftRoster.Create(TenantId, EmployeeId, ShiftId, period, Now);
        _rosterReader.ListAsync(Arg.Any<ISpecification<ShiftRoster>>(), Arg.Any<CancellationToken>()).Returns([roster]);
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(CreateUser());

        var command = new PublishRosterCommand([EmployeeId.Value], new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 7));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        roster.Status.Should().Be(ShiftRosterStatus.Published);
        _rosterWriter.Received(1).Update(roster);
        await _notificationDispatcher.Received(1).DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Skip_Rows_That_Do_Not_Overlap_The_Range()
    {
        var period = DateRange.Create(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 7)).Value;
        var roster = ShiftRoster.Create(TenantId, EmployeeId, ShiftId, period, Now);
        _rosterReader.ListAsync(Arg.Any<ISpecification<ShiftRoster>>(), Arg.Any<CancellationToken>()).Returns([roster]);

        var command = new PublishRosterCommand([EmployeeId.Value], new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 7));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        roster.Status.Should().Be(ShiftRosterStatus.Draft);
        await _notificationDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Not_Notify_When_No_Employee_Had_Any_Row_Published()
    {
        _rosterReader.ListAsync(Arg.Any<ISpecification<ShiftRoster>>(), Arg.Any<CancellationToken>()).Returns([]);

        var command = new PublishRosterCommand([EmployeeId.Value], new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 7));
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        await _notificationDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }
}
