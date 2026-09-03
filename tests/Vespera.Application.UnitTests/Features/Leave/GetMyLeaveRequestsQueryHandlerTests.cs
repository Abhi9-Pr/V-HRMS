using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class GetMyLeaveRequestsQueryHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private GetMyLeaveRequestsQueryHandler CreateHandler() => new(_users, _leaveRequests, _tenantContext, _currentUser);

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    [Fact]
    public async Task Handle_Should_Return_The_Callers_LeaveRequests()
    {
        var employeeId = EmployeeId.New();
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employeeId));
        var leaveRequest = LeaveRequest.Submit(
            _tenantId, employeeId, LeaveTypeId.New(), DateRange.Create(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6)).Value, 2,
            "Vacation", Now, "system").Value;
        _leaveRequests.ListAsync(Arg.Any<LeaveRequestsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { leaveRequest });

        var result = await CreateHandler().Handle(new GetMyLeaveRequestsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.Id == leaveRequest.Id.Value && d.Status == "Pending");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetMyLeaveRequestsQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_Caller_Has_No_Employee_Profile()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(null));

        var result = await CreateHandler().Handle(new GetMyLeaveRequestsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
