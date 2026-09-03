using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class GetMyLeaveDeltaSyncQueryHandlerTests
{
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    private GetMyLeaveDeltaSyncQueryHandler CreateHandler() =>
        new(_leaveRequests, _dateTimeProvider, _tenantContext, new CurrentEmployeeResolver(_users, _currentUser));

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    private static LeaveRequest CreateLeaveRequest(TenantId tenantId, EmployeeId employeeId, DateTimeOffset submittedAt) =>
        LeaveRequest.Submit(
            tenantId, employeeId, LeaveTypeId.New(), DateRange.Create(new DateOnly(2026, 1, 20), new DateOnly(2026, 1, 21)).Value, 2,
            "Vacation", submittedAt, "system").Value;

    [Fact]
    public async Task Handle_Should_Return_Requests_Changed_Since_The_Given_Cutoff()
    {
        var employeeId = EmployeeId.New();
        _currentUser.UserId.Returns(Guid.NewGuid());
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employeeId));
        var changed = CreateLeaveRequest(_tenantId, employeeId, Now.AddDays(-1));
        var unchanged = CreateLeaveRequest(_tenantId, employeeId, Now.AddDays(-10));
        _leaveRequests.ListAsync(Arg.Any<LeaveRequestsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { changed, unchanged });

        var query = new GetMyLeaveDeltaSyncQuery(Now.AddDays(-5), null, 100);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Upserts.Should().ContainSingle(d => d.Id == changed.Id.Value);
        result.Value.TombstonedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Set_NextCursor_When_More_Results_Exist_Beyond_The_Page()
    {
        var employeeId = EmployeeId.New();
        _currentUser.UserId.Returns(Guid.NewGuid());
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employeeId));
        var first = CreateLeaveRequest(_tenantId, employeeId, Now.AddDays(-3));
        var second = CreateLeaveRequest(_tenantId, employeeId, Now.AddDays(-1));
        _leaveRequests.ListAsync(Arg.Any<LeaveRequestsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { first, second });

        var query = new GetMyLeaveDeltaSyncQuery(Now.AddDays(-5), null, 1);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Upserts.Should().HaveCount(1);
        result.Value.NextCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_Result_When_Caller_Has_No_Linked_Employee()
    {
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(Now);
        _leaveRequests.ListAsync(Arg.Any<LeaveRequestsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest>());

        var query = new GetMyLeaveDeltaSyncQuery(Now.AddDays(-5), null, 100);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Upserts.Should().BeEmpty();
    }
}
