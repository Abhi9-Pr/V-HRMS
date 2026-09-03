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

public class GetApprovalInboxQueryHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<ApprovalChain> _chains = Substitute.For<IReadRepository<ApprovalChain>>();
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly IReadRepository<ProxyDelegation> _delegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public GetApprovalInboxQueryHandlerTests()
    {
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProxyDelegation>());
    }

    private GetApprovalInboxQueryHandler CreateHandler() => new(
        _users, _chains, _leaveRequests, new LeaveApprovalStepAuthorizer(_delegations), _tenantContext, _currentUser, _dateTimeProvider);

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    private LeaveRequest CreateLeaveRequest(EmployeeId employeeId) => LeaveRequest.Submit(
        _tenantId, employeeId, LeaveTypeId.New(), DateRange.Create(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6)).Value, 2,
        "Vacation", Now, "system").Value;

    [Fact]
    public async Task Handle_Should_Return_Chains_Currently_Awaiting_The_Callers_Decision()
    {
        var approverId = EmployeeId.New();
        var leaveRequest = CreateLeaveRequest(EmployeeId.New());
        var chain = ApprovalChain.Create(_tenantId, ApprovalSubjectType.LeaveRequest, leaveRequest.Id.Value, [approverId], Now).Value;
        var userId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(approverId));
        _chains.ListAsync(Arg.Any<InProgressApprovalChainsBySubjectTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApprovalChain> { chain });
        _leaveRequests.FirstOrDefaultAsync(Arg.Any<LeaveRequestByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveRequest);

        var result = await CreateHandler().Handle(new GetApprovalInboxQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.LeaveRequestId == leaveRequest.Id.Value && !d.ActingAsDelegate);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetApprovalInboxQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_Caller_Has_No_Employee_Profile()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(null));

        var result = await CreateHandler().Handle(new GetApprovalInboxQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Skip_Chains_Not_Currently_Awaiting_The_Callers_Decision()
    {
        var callerEmployeeId = EmployeeId.New();
        var otherApproverId = EmployeeId.New();
        var leaveRequest = CreateLeaveRequest(EmployeeId.New());
        var chain = ApprovalChain.Create(_tenantId, ApprovalSubjectType.LeaveRequest, leaveRequest.Id.Value, [otherApproverId], Now).Value;
        var userId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(callerEmployeeId));
        _chains.ListAsync(Arg.Any<InProgressApprovalChainsBySubjectTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApprovalChain> { chain });

        var result = await CreateHandler().Handle(new GetApprovalInboxQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
