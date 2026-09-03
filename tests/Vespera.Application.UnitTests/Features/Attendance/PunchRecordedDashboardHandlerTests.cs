using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Attendance;
using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class PunchRecordedDashboardHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private readonly IReadRepositoryAdmin<User> _usersAdmin = Substitute.For<IReadRepositoryAdmin<User>>();
    private readonly IDashboardRealtimePublisher _publisher = Substitute.For<IDashboardRealtimePublisher>();

    private PunchRecordedDashboardHandler CreateHandler() => new(_usersAdmin, _publisher);

    [Fact]
    public async Task Handle_Should_Publish_The_Punch_State_To_The_Employees_User()
    {
        var employeeId = EmployeeId.New();
        var user = User.Create(TenantId, EmailAddress.Create("employee@vespera.test").Value, employeeId, Now, "system");
        _usersAdmin.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([user]);

        var notification = new DomainEventNotification<PunchRecorded>(new PunchRecorded(TenantId, employeeId, Now, PunchType.In, Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _publisher.Received(1).PublishPunchStateChangedAsync(
            user.Id.Value,
            Arg.Is<PunchStateChangedPayload>(p => p.EmployeeId == employeeId.Value && p.Status == "In" && p.LastPunchAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Employee_Has_No_Linked_User()
    {
        _usersAdmin.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var notification = new DomainEventNotification<PunchRecorded>(new PunchRecorded(TenantId, EmployeeId.New(), Now, PunchType.Out, Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _publisher.DidNotReceive().PublishPunchStateChangedAsync(
            Arg.Any<Guid>(), Arg.Any<PunchStateChangedPayload>(), Arg.Any<CancellationToken>());
    }
}
