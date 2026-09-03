using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Eis;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Leave.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class LeaveApprovedNotificationHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepositoryAdmin<User> _usersAdmin = Substitute.For<IReadRepositoryAdmin<User>>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();

    private LeaveApprovedNotificationHandler CreateHandler() => new(_usersAdmin, _dispatcher);

    private static LeaveApproved CreateEvent(EmployeeId employeeId) => new(
        LeaveRequestId.New(), TenantId, employeeId, DateRange.Create(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6)).Value,
        EmployeeId.New(), Now);

    [Fact]
    public async Task Handle_Should_Dispatch_A_Notification_To_The_Requesting_Employees_User()
    {
        var employeeId = EmployeeId.New();
        var user = User.Create(TenantId, EmailAddress.Create("employee@vespera.test").Value, employeeId, Now, "system");
        _usersAdmin.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([user]);

        var notification = new DomainEventNotification<LeaveApproved>(CreateEvent(employeeId));
        await CreateHandler().Handle(notification, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<NotificationMessage>(m => m.RecipientId == user.Id.Value.ToString()), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Employee_Has_No_Linked_User()
    {
        _usersAdmin.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var notification = new DomainEventNotification<LeaveApproved>(CreateEvent(EmployeeId.New()));
        await CreateHandler().Handle(notification, CancellationToken.None);

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }
}
