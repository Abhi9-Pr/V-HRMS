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

public class EncashLeaveCommandHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<LeaveType> _leaveTypes = Substitute.For<IReadRepository<LeaveType>>();
    private readonly IReadRepository<LeaveBalance> _balances = Substitute.For<IReadRepository<LeaveBalance>>();
    private readonly IWriteRepository<LeaveBalance> _balanceWriter = Substitute.For<IWriteRepository<LeaveBalance>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private EncashLeaveCommandHandler CreateHandler() =>
        new(_users, _leaveTypes, _balances, _balanceWriter, _tenantContext, _currentUser, _dateTimeProvider);

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    private static LeaveType CreateEncashableLeaveType(decimal maxEncashableDays)
    {
        var leaveType = LeaveType.Create(TenantId.New(), "Earned Leave", true, 0, Now, "system").Value;
        leaveType.UpdateEligibilityRules(null, 0, true, maxEncashableDays, Now, "system");
        return leaveType;
    }

    private void SetupAuthenticatedCaller(EmployeeId employeeId)
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employeeId));
    }

    [Fact]
    public async Task Handle_Should_Encash_When_Balance_Is_Sufficient()
    {
        var employeeId = EmployeeId.New();
        SetupAuthenticatedCaller(employeeId);
        var leaveType = CreateEncashableLeaveType(10);
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);
        var balance = LeaveBalance.Open(_tenantId, employeeId, leaveType.Id);
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 10m, "Monthly accrual", Now, "system");
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>()).Returns(balance);

        var result = await CreateHandler().Handle(new EncashLeaveCommand(leaveType.Id.Value, 4), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        balance.Available.Should().Be(6m);
        _balanceWriter.Received(1).Update(balance);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new EncashLeaveCommand(Guid.NewGuid(), 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_encashment.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Caller_Has_No_Employee_Profile()
    {
        SetupAuthenticatedCaller(EmployeeId.New());
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(null));

        var result = await CreateHandler().Handle(new EncashLeaveCommand(Guid.NewGuid(), 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_encashment.no_employee_profile");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_LeaveType_Not_Found()
    {
        SetupAuthenticatedCaller(EmployeeId.New());
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((LeaveType?)null);

        var result = await CreateHandler().Handle(new EncashLeaveCommand(Guid.NewGuid(), 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_encashment.leave_type_not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_LeaveType_Is_Not_Encashable()
    {
        SetupAuthenticatedCaller(EmployeeId.New());
        var leaveType = LeaveType.Create(TenantId.New(), "Sick Leave", true, 0, Now, "system").Value;
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);

        var result = await CreateHandler().Handle(new EncashLeaveCommand(leaveType.Id.Value, 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_encashment.not_encashable");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Days_Exceeds_MaxEncashableDays()
    {
        SetupAuthenticatedCaller(EmployeeId.New());
        var leaveType = CreateEncashableLeaveType(3);
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);

        var result = await CreateHandler().Handle(new EncashLeaveCommand(leaveType.Id.Value, 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_encashment.exceeds_maximum");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Balance_Is_Insufficient()
    {
        var employeeId = EmployeeId.New();
        SetupAuthenticatedCaller(employeeId);
        var leaveType = CreateEncashableLeaveType(10);
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);
        var balance = LeaveBalance.Open(_tenantId, employeeId, leaveType.Id);
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 2m, "Monthly accrual", Now, "system");
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>()).Returns(balance);

        var result = await CreateHandler().Handle(new EncashLeaveCommand(leaveType.Id.Value, 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_encashment.insufficient_balance");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Balance_Exists()
    {
        SetupAuthenticatedCaller(EmployeeId.New());
        var leaveType = CreateEncashableLeaveType(10);
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>()).Returns((LeaveBalance?)null);

        var result = await CreateHandler().Handle(new EncashLeaveCommand(leaveType.Id.Value, 4), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_encashment.insufficient_balance");
    }
}
