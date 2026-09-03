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

public class GetLeaveBalanceQueryHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<LeaveBalance> _balances = Substitute.For<IReadRepository<LeaveBalance>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private GetLeaveBalanceQueryHandler CreateHandler() => new(_users, _balances, _tenantContext, _currentUser);

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    [Fact]
    public async Task Handle_Should_Return_The_Balance_Projection_For_The_Caller()
    {
        var employeeId = EmployeeId.New();
        var leaveTypeId = LeaveTypeId.New();
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employeeId));
        var balance = LeaveBalance.Open(_tenantId, employeeId, leaveTypeId);
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 10m, "Monthly accrual", Now, "system");
        balance.PostEntry(LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 2m, "Leave taken", Now, "system");
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>()).Returns(balance);

        var result = await CreateHandler().Handle(new GetLeaveBalanceQuery(leaveTypeId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Available.Should().Be(8m);
        result.Value.Accrued.Should().Be(10m);
        result.Value.Used.Should().Be(2m);
        result.Value.RecentEntries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetLeaveBalanceQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_balance.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Return_A_Zeroed_Balance_When_Caller_Has_No_Employee_Profile()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(null));

        var result = await CreateHandler().Handle(new GetLeaveBalanceQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Available.Should().Be(0m);
        result.Value.RecentEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Return_A_Zeroed_Balance_When_No_Balance_Exists_Yet()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(EmployeeId.New()));
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((LeaveBalance?)null);

        var result = await CreateHandler().Handle(new GetLeaveBalanceQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Available.Should().Be(0m);
    }
}
