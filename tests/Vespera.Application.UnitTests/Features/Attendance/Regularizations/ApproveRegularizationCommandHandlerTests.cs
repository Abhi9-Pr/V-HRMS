using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance.Regularizations;

public class ApproveRegularizationCommandHandlerTests
{
    private readonly IReadRepository<RegularizationRequest> _requests = Substitute.For<IReadRepository<RegularizationRequest>>();
    private readonly IWriteRepository<RegularizationRequest> _requestWriter = Substitute.For<IWriteRepository<RegularizationRequest>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();
    private readonly IReadRepository<ProxyDelegation> _delegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _requestingEmployeeId = EmployeeId.New();
    private readonly EmployeeId _nominalManagerId = EmployeeId.New();
    private readonly EmployeeId _delegateId = EmployeeId.New();
    private readonly DateTimeOffset _now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    public ApproveRegularizationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(_now);

        var managerRelationship = ReportingRelationship.Create(
            _tenantId, _requestingEmployeeId, _nominalManagerId, new DateOnly(2020, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns(managerRelationship);
    }

    private ApproveRegularizationCommandHandler CreateHandler() => new(
        _requests, _requestWriter, _users, new RegularizationApproverResolver(_reportingRelationships, _delegations),
        _tenantContext, _currentUser, _dateTimeProvider);

    private RegularizationRequest CreatePendingRequest() =>
        RegularizationRequest.Submit(_tenantId, _requestingEmployeeId, AttendanceDayId.New(), "Forgot to punch out").Value;

    private User CreateUser(EmployeeId? employeeId) =>
        User.Create(_tenantId, EmailAddress.Create($"{Guid.NewGuid():N}@vespera.test").Value, employeeId, _now, "seed");

    [Fact]
    public async Task Handle_Should_Succeed_When_Caller_Is_The_Nominal_Manager_And_No_Delegation_Is_Active()
    {
        _delegations.ListAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);
        var request = CreatePendingRequest();
        var callerUser = CreateUser(_nominalManagerId);
        _requests.FirstOrDefaultAsync(Arg.Any<ISpecification<RegularizationRequest>>(), Arg.Any<CancellationToken>()).Returns(request);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(callerUser);

        var result = await CreateHandler().Handle(new ApproveRegularizationCommand(request.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(RegularizationStatus.Approved);
    }

    [Fact]
    public async Task Handle_Should_Let_The_Active_Delegate_Approve_And_Should_Forbid_The_Nominal_Manager()
    {
        var validity = DateRange.Create(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)).Value;
        var delegation = ProxyDelegation.Create(_tenantId, _nominalManagerId, _delegateId, validity, DelegationScope.AttendanceApprovals).Value;
        _delegations.ListAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([delegation]);

        // The delegate's Approve call should succeed.
        var requestForDelegate = CreatePendingRequest();
        var delegateUser = CreateUser(_delegateId);
        _requests.FirstOrDefaultAsync(Arg.Any<ISpecification<RegularizationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(requestForDelegate);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(delegateUser);

        var delegateResult = await CreateHandler().Handle(new ApproveRegularizationCommand(requestForDelegate.Id.Value), CancellationToken.None);

        delegateResult.IsSuccess.Should().BeTrue();
        requestForDelegate.Status.Should().Be(RegularizationStatus.Approved);
        requestForDelegate.ApproverId.Should().Be(_delegateId);

        // The nominal manager's Approve call, against a fresh pending request, should now be Forbidden.
        var requestForManager = CreatePendingRequest();
        var managerUser = CreateUser(_nominalManagerId);
        _requests.FirstOrDefaultAsync(Arg.Any<ISpecification<RegularizationRequest>>(), Arg.Any<CancellationToken>())
            .Returns(requestForManager);
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(managerUser);

        var managerResult = await CreateHandler().Handle(new ApproveRegularizationCommand(requestForManager.Id.Value), CancellationToken.None);

        managerResult.IsFailure.Should().BeTrue();
        managerResult.Error.Code.Should().Be("regularization_request.not_authorized_approver");
        requestForManager.Status.Should().Be(RegularizationStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_Forbid_A_Caller_Who_Is_Neither_The_Manager_Nor_A_Delegate()
    {
        _delegations.ListAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);
        var request = CreatePendingRequest();
        var strangerUser = CreateUser(EmployeeId.New());
        _requests.FirstOrDefaultAsync(Arg.Any<ISpecification<RegularizationRequest>>(), Arg.Any<CancellationToken>()).Returns(request);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(strangerUser);

        var result = await CreateHandler().Handle(new ApproveRegularizationCommand(request.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("regularization_request.not_authorized_approver");
    }
}
