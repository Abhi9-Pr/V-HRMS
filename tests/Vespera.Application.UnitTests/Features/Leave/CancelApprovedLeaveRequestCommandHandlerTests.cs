using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CancelApprovedLeaveRequestCommandHandlerTests
{
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly IWriteRepository<LeaveRequest> _leaveRequestWriter = Substitute.For<IWriteRepository<LeaveRequest>>();
    private readonly IReadRepository<LeaveBalance> _balances = Substitute.For<IReadRepository<LeaveBalance>>();
    private readonly IWriteRepository<LeaveBalance> _balanceWriter = Substitute.For<IWriteRepository<LeaveBalance>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private CancelApprovedLeaveRequestCommandHandler CreateHandler() =>
        new(_leaveRequests, _leaveRequestWriter, _balances, _balanceWriter, _users, _tenantContext, _currentUser, _dateTimeProvider);

    private LeaveRequest CreateApprovedLeaveRequest(EmployeeId employeeId, LeaveTypeId leaveTypeId, DateOnly from, DateOnly to)
    {
        var leaveRequest = LeaveRequest.Submit(
            _tenantId, employeeId, leaveTypeId, DateRange.Create(from, to).Value, 2, "Vacation", Now, "system").Value;
        leaveRequest.Approve(EmployeeId.New(), Now, "manager");
        return leaveRequest;
    }

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    [Fact]
    public async Task Handle_Should_Cancel_The_LeaveRequest_And_Reverse_The_Debit()
    {
        var employeeId = EmployeeId.New();
        var leaveTypeId = LeaveTypeId.New();
        var leaveRequest = CreateApprovedLeaveRequest(employeeId, leaveTypeId, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 2));
        var balance = LeaveBalance.Open(_tenantId, employeeId, leaveTypeId);
        balance.PostEntry(
            LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 2m, "Leave request submitted", Now, "system",
            sourceType: "LeaveRequest", sourceId: leaveRequest.Id.Value);
        var userId = Guid.NewGuid();

        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>()).Returns(balance);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employeeId));

        var result = await CreateHandler().Handle(
            new CancelApprovedLeaveRequestCommand(leaveRequest.Id.Value, "Plans changed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leaveRequest.Status.Should().Be(LeaveRequestStatus.Cancelled);
        balance.Available.Should().Be(0m);
        _leaveRequestWriter.Received(1).Update(leaveRequest);
        _balanceWriter.Received(1).Update(balance);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_LeaveRequest_Not_Found()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);

        var result = await CreateHandler().Handle(new CancelApprovedLeaveRequestCommand(Guid.NewGuid(), "Plans changed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        var leaveRequest = CreateApprovedLeaveRequest(EmployeeId.New(), LeaveTypeId.New(), new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 2));
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new CancelApprovedLeaveRequestCommand(leaveRequest.Id.Value, "Plans changed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Caller_Is_Not_The_Requester()
    {
        var leaveRequest = CreateApprovedLeaveRequest(EmployeeId.New(), LeaveTypeId.New(), new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 2));
        var userId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(EmployeeId.New()));

        var result = await CreateHandler().Handle(
            new CancelApprovedLeaveRequestCommand(leaveRequest.Id.Value, "Plans changed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_the_requester");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_LeaveRequest_Is_Not_Approved()
    {
        var employeeId = EmployeeId.New();
        var leaveRequest = LeaveRequest.Submit(
            _tenantId, employeeId, LeaveTypeId.New(), DateRange.Create(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 2)).Value, 2,
            "Vacation", Now, "system").Value;
        var userId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employeeId));

        var result = await CreateHandler().Handle(
            new CancelApprovedLeaveRequestCommand(leaveRequest.Id.Value, "Plans changed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_approved");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Leave_Has_Already_Started()
    {
        var employeeId = EmployeeId.New();
        var leaveRequest = CreateApprovedLeaveRequest(employeeId, LeaveTypeId.New(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2));
        var userId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employeeId));

        var result = await CreateHandler().Handle(
            new CancelApprovedLeaveRequestCommand(leaveRequest.Id.Value, "Plans changed"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.already_started");
    }
}
