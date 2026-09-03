using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class RejectLeaveRequestCommandHandlerTests
{
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly IWriteRepository<LeaveRequest> _leaveRequestWriter = Substitute.For<IWriteRepository<LeaveRequest>>();
    private readonly IReadRepository<ApprovalChain> _chains = Substitute.For<IReadRepository<ApprovalChain>>();
    private readonly IWriteRepository<ApprovalChain> _chainWriter = Substitute.For<IWriteRepository<ApprovalChain>>();
    private readonly IReadRepository<LeaveBalance> _balances = Substitute.For<IReadRepository<LeaveBalance>>();
    private readonly IWriteRepository<LeaveBalance> _balanceWriter = Substitute.For<IWriteRepository<LeaveBalance>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<ProxyDelegation> _delegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public RejectLeaveRequestCommandHandlerTests()
    {
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProxyDelegation>());
    }

    private RejectLeaveRequestCommandHandler CreateHandler() => new(
        _leaveRequests, _leaveRequestWriter, _chains, _chainWriter, _balances, _balanceWriter, _users,
        new LeaveApprovalStepAuthorizer(_delegations), _tenantContext, _currentUser, _dateTimeProvider);

    private LeaveRequest CreateLeaveRequest(EmployeeId employeeId, LeaveTypeId leaveTypeId) => LeaveRequest.Submit(
        _tenantId, employeeId, leaveTypeId, DateRange.Create(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6)).Value, 2,
        "Vacation", Now, "system").Value;

    private static ApprovalChain CreateChain(Guid subjectId, EmployeeId approverId) =>
        ApprovalChain.Create(TenantId.New(), ApprovalSubjectType.LeaveRequest, subjectId, [approverId], Now).Value;

    private User CreateApproverUser(EmployeeId employeeId)
    {
        var email = EmailAddress.Create("manager@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    [Fact]
    public async Task Handle_Should_Reject_The_LeaveRequest_And_Reverse_The_Debit()
    {
        var approverId = EmployeeId.New();
        var employeeId = EmployeeId.New();
        var leaveTypeId = LeaveTypeId.New();
        var leaveRequest = CreateLeaveRequest(employeeId, leaveTypeId);
        var chain = CreateChain(leaveRequest.Id.Value, approverId);
        var balance = LeaveBalance.Open(_tenantId, employeeId, leaveTypeId);
        balance.PostEntry(
            LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 2m, "Leave request submitted", Now, "system",
            sourceType: "LeaveRequest", sourceId: leaveRequest.Id.Value);
        var userId = Guid.NewGuid();

        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _chains.FirstOrDefaultAsync(Arg.Any<ApprovalChainBySubjectSpecification>(), Arg.Any<CancellationToken>()).Returns(chain);
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>()).Returns(balance);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateApproverUser(approverId));

        var result = await CreateHandler().Handle(new RejectLeaveRequestCommand(leaveRequest.Id.Value, "Not eligible"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leaveRequest.Status.Should().Be(LeaveRequestStatus.Rejected);
        chain.Status.Should().Be(ApprovalChainStatus.Rejected);
        balance.Available.Should().Be(0m);
        _leaveRequestWriter.Received(1).Update(leaveRequest);
        _chainWriter.Received(1).Update(chain);
        _balanceWriter.Received(1).Update(balance);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_LeaveRequest_Not_Found()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);

        var result = await CreateHandler().Handle(new RejectLeaveRequestCommand(Guid.NewGuid(), "Not eligible"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Chain_Not_Found()
    {
        var leaveRequest = CreateLeaveRequest(EmployeeId.New(), LeaveTypeId.New());
        _tenantContext.TenantId.Returns(_tenantId);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _chains.FirstOrDefaultAsync(Arg.Any<ApprovalChainBySubjectSpecification>(), Arg.Any<CancellationToken>()).Returns((ApprovalChain?)null);

        var result = await CreateHandler().Handle(new RejectLeaveRequestCommand(leaveRequest.Id.Value, "Not eligible"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.chain_not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        var leaveRequest = CreateLeaveRequest(EmployeeId.New(), LeaveTypeId.New());
        var chain = CreateChain(leaveRequest.Id.Value, EmployeeId.New());
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _chains.FirstOrDefaultAsync(Arg.Any<ApprovalChainBySubjectSpecification>(), Arg.Any<CancellationToken>()).Returns(chain);
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new RejectLeaveRequestCommand(leaveRequest.Id.Value, "Not eligible"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Caller_Is_Not_The_Authorized_Approver()
    {
        var leaveRequest = CreateLeaveRequest(EmployeeId.New(), LeaveTypeId.New());
        var chain = CreateChain(leaveRequest.Id.Value, EmployeeId.New());
        var userId = Guid.NewGuid();

        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _chains.FirstOrDefaultAsync(Arg.Any<ApprovalChainBySubjectSpecification>(), Arg.Any<CancellationToken>()).Returns(chain);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateApproverUser(EmployeeId.New()));

        var result = await CreateHandler().Handle(new RejectLeaveRequestCommand(leaveRequest.Id.Value, "Not eligible"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_authorized_approver");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Reason_Is_Blank()
    {
        var approverId = EmployeeId.New();
        var leaveRequest = CreateLeaveRequest(EmployeeId.New(), LeaveTypeId.New());
        var chain = CreateChain(leaveRequest.Id.Value, approverId);
        var userId = Guid.NewGuid();

        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);
        _chains.FirstOrDefaultAsync(Arg.Any<ApprovalChainBySubjectSpecification>(), Arg.Any<CancellationToken>()).Returns(chain);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateApproverUser(approverId));

        var result = await CreateHandler().Handle(new RejectLeaveRequestCommand(leaveRequest.Id.Value, "   "), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.rejection_reason_required");
    }
}
