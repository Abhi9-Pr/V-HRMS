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

public class ApprovalStepAssignedNotificationHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepositoryAdmin<LeaveRequest> _leaveRequestsAdmin = Substitute.For<IReadRepositoryAdmin<LeaveRequest>>();
    private readonly IReadRepositoryAdmin<ProxyDelegation> _delegationsAdmin = Substitute.For<IReadRepositoryAdmin<ProxyDelegation>>();
    private readonly IReadRepositoryAdmin<User> _usersAdmin = Substitute.For<IReadRepositoryAdmin<User>>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();

    private ApprovalStepAssignedNotificationHandler CreateHandler() => new(_leaveRequestsAdmin, _delegationsAdmin, _usersAdmin, _dispatcher);

    private static LeaveRequest CreateLeaveRequest() => LeaveRequest.Submit(
        TenantId, EmployeeId.New(), LeaveTypeId.New(), DateRange.Create(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6)).Value, 2,
        "Vacation", Now, "system").Value;

    [Fact]
    public async Task Handle_Should_Dispatch_A_Notification_To_The_Nominal_Approver()
    {
        var approver = EmployeeId.New();
        var leaveRequest = CreateLeaveRequest();
        var user = User.Create(TenantId, EmailAddress.Create("approver@vespera.test").Value, approver, Now, "system");
        _leaveRequestsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<LeaveRequest>>(), Arg.Any<CancellationToken>())
            .Returns([leaveRequest]);
        _delegationsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);
        _usersAdmin.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([user]);

        var notification = new DomainEventNotification<ApprovalStepAssigned>(
            new ApprovalStepAssigned(ApprovalChainId.New(), TenantId, ApprovalSubjectType.LeaveRequest, leaveRequest.Id.Value, 0, approver, Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<NotificationMessage>(m => m.RecipientId == user.Id.Value.ToString()), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_SubjectType_Is_Not_LeaveRequest()
    {
        var notification = new DomainEventNotification<ApprovalStepAssigned>(
            new ApprovalStepAssigned(ApprovalChainId.New(), TenantId, ApprovalSubjectType.ExpenseClaim, Guid.NewGuid(), 0, EmployeeId.New(), Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_LeaveRequest_Is_Not_Found()
    {
        _leaveRequestsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<LeaveRequest>>(), Arg.Any<CancellationToken>()).Returns([]);

        var notification = new DomainEventNotification<ApprovalStepAssigned>(
            new ApprovalStepAssigned(ApprovalChainId.New(), TenantId, ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), 0, EmployeeId.New(), Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Approver_Has_No_Linked_User()
    {
        var leaveRequest = CreateLeaveRequest();
        _leaveRequestsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<LeaveRequest>>(), Arg.Any<CancellationToken>())
            .Returns([leaveRequest]);
        _delegationsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);
        _usersAdmin.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var notification = new DomainEventNotification<ApprovalStepAssigned>(
            new ApprovalStepAssigned(
                ApprovalChainId.New(), TenantId, ApprovalSubjectType.LeaveRequest, leaveRequest.Id.Value, 0, EmployeeId.New(), Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Dispatch_To_The_Delegate_When_An_Active_Delegation_Exists()
    {
        var nominalApprover = EmployeeId.New();
        var delegateApprover = EmployeeId.New();
        var leaveRequest = CreateLeaveRequest();
        var delegateUser = User.Create(TenantId, EmailAddress.Create("delegate@vespera.test").Value, delegateApprover, Now, "system");
        var delegation = ProxyDelegation.Create(
            TenantId, nominalApprover, delegateApprover, DateRange.Create(new DateOnly(2025, 12, 25), new DateOnly(2026, 1, 10)).Value,
            DelegationScope.LeaveApprovals).Value;

        _leaveRequestsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<LeaveRequest>>(), Arg.Any<CancellationToken>())
            .Returns([leaveRequest]);
        _delegationsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>())
            .Returns([delegation]);
        _usersAdmin.ListIgnoringFiltersAsync(
                Arg.Is<UserByEmployeeIdSpecification>(_ => true), Arg.Any<CancellationToken>())
            .Returns([delegateUser]);

        var notification = new DomainEventNotification<ApprovalStepAssigned>(
            new ApprovalStepAssigned(
                ApprovalChainId.New(), TenantId, ApprovalSubjectType.LeaveRequest, leaveRequest.Id.Value, 0, nominalApprover, Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<NotificationMessage>(m => m.RecipientId == delegateUser.Id.Value.ToString()), Arg.Any<CancellationToken>());
    }
}
