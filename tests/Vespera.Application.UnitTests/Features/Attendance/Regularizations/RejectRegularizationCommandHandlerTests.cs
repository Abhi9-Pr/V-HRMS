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

public class RejectRegularizationCommandHandlerTests
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
    private readonly DateTimeOffset _now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    public RejectRegularizationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(_now);
        _delegations.ListAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);

        var managerRelationship = ReportingRelationship.Create(
            _tenantId, _requestingEmployeeId, _nominalManagerId, new DateOnly(2020, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns(managerRelationship);
    }

    private RejectRegularizationCommandHandler CreateHandler() => new(
        _requests, _requestWriter, _users, new RegularizationApproverResolver(_reportingRelationships, _delegations),
        _tenantContext, _currentUser, _dateTimeProvider);

    private RegularizationRequest CreatePendingRequest() =>
        RegularizationRequest.Submit(_tenantId, _requestingEmployeeId, AttendanceDayId.New(), "Forgot to punch out").Value;

    [Fact]
    public async Task Handle_Should_Succeed_When_Caller_Is_The_Nominal_Manager()
    {
        var request = CreatePendingRequest();
        var callerUser = User.Create(_tenantId, EmailAddress.Create("m@vespera.test").Value, _nominalManagerId, _now, "seed");
        _requests.FirstOrDefaultAsync(Arg.Any<ISpecification<RegularizationRequest>>(), Arg.Any<CancellationToken>()).Returns(request);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(callerUser);

        var result = await CreateHandler().Handle(
            new RejectRegularizationCommand(request.Id.Value, "Not supported by evidence"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(RegularizationStatus.Rejected);
        request.RejectionReason.Should().Be("Not supported by evidence");
    }

    [Fact]
    public async Task Handle_Should_Forbid_A_Caller_Who_Is_Not_The_Authorized_Approver()
    {
        var request = CreatePendingRequest();
        var strangerUser = User.Create(_tenantId, EmailAddress.Create("s@vespera.test").Value, EmployeeId.New(), _now, "seed");
        _requests.FirstOrDefaultAsync(Arg.Any<ISpecification<RegularizationRequest>>(), Arg.Any<CancellationToken>()).Returns(request);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(strangerUser);

        var result = await CreateHandler().Handle(
            new RejectRegularizationCommand(request.Id.Value, "Not supported by evidence"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("regularization_request.not_authorized_approver");
        request.Status.Should().Be(RegularizationStatus.Pending);
    }
}
