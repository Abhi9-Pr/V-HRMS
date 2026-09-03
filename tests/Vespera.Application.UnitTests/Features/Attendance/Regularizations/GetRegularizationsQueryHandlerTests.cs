using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance.Regularizations;

public class GetRegularizationsQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IReadRepository<RegularizationRequest> _requests = Substitute.For<IReadRepository<RegularizationRequest>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();
    private readonly IReadRepository<ProxyDelegation> _delegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public GetRegularizationsQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private GetRegularizationsQueryHandler CreateHandler() =>
        new(_requests, _users, new RegularizationApproverResolver(_reportingRelationships, _delegations), _tenantContext, _currentUser, _dateTimeProvider);

    private void SignInAs(EmployeeId employeeId)
    {
        var userId = Guid.NewGuid();
        var user = User.Create(TenantId, EmailAddress.Create("caller@vespera.test").Value, employeeId, Now, "system");
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_There_Is_No_Signed_In_User()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetRegularizationsQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("regularization_request.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_The_Callers_Account_Has_No_Linked_Employee()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new GetRegularizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Include_Requests_Whose_Employees_Manager_Is_The_Caller()
    {
        var manager = EmployeeId.New();
        var subordinate = EmployeeId.New();
        SignInAs(manager);
        var request = RegularizationRequest.Submit(TenantId, subordinate, AttendanceDayId.New(), "Forgot to punch out").Value;
        var relationship = ReportingRelationship.Create(TenantId, subordinate, manager, Today.AddYears(-1), null).Value;

        _requests.ListAsync(Arg.Any<PendingRegularizationRequestsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([request]);
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(relationship);

        var result = await CreateHandler().Handle(new GetRegularizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == request.Id.Value && dto.EmployeeId == subordinate.Value);
    }

    [Fact]
    public async Task Handle_Should_Include_Requests_Delegated_To_The_Caller()
    {
        var nominalManager = EmployeeId.New();
        var delegate_ = EmployeeId.New();
        var subordinate = EmployeeId.New();
        SignInAs(delegate_);
        var request = RegularizationRequest.Submit(TenantId, subordinate, AttendanceDayId.New(), "Forgot to punch out").Value;
        var relationship = ReportingRelationship.Create(TenantId, subordinate, nominalManager, Today.AddYears(-1), null).Value;
        var delegation = ProxyDelegation.Create(
            TenantId, nominalManager, delegate_, DateRange.Create(Today.AddDays(-1), Today.AddDays(1)).Value, DelegationScope.AttendanceApprovals).Value;

        _requests.ListAsync(Arg.Any<PendingRegularizationRequestsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([request]);
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(relationship);
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>()).Returns([delegation]);

        var result = await CreateHandler().Handle(new GetRegularizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == request.Id.Value);
    }

    [Fact]
    public async Task Handle_Should_Exclude_Requests_Whose_Employee_Has_No_Manager_Relationship()
    {
        var caller = EmployeeId.New();
        SignInAs(caller);
        var request = RegularizationRequest.Submit(TenantId, EmployeeId.New(), AttendanceDayId.New(), "Forgot to punch out").Value;

        _requests.ListAsync(Arg.Any<PendingRegularizationRequestsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([request]);
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns((ReportingRelationship?)null);

        var result = await CreateHandler().Handle(new GetRegularizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Exclude_Requests_Whose_Manager_Is_Someone_Else()
    {
        var caller = EmployeeId.New();
        SignInAs(caller);
        var subordinate = EmployeeId.New();
        var request = RegularizationRequest.Submit(TenantId, subordinate, AttendanceDayId.New(), "Forgot to punch out").Value;
        var relationship = ReportingRelationship.Create(TenantId, subordinate, EmployeeId.New(), Today.AddYears(-1), null).Value;

        _requests.ListAsync(Arg.Any<PendingRegularizationRequestsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([request]);
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(relationship);

        var result = await CreateHandler().Handle(new GetRegularizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
