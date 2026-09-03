using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Eis;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class SubmitLeaveRequestCommandHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<LeaveType> _leaveTypes = Substitute.For<IReadRepository<LeaveType>>();
    private readonly IReadRepository<LeavePolicy> _leavePolicies = Substitute.For<IReadRepository<LeavePolicy>>();
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly IWriteRepository<LeaveRequest> _leaveRequestWriter = Substitute.For<IWriteRepository<LeaveRequest>>();
    private readonly IReadRepository<LeaveBalance> _balances = Substitute.For<IReadRepository<LeaveBalance>>();
    private readonly IWriteRepository<LeaveBalance> _balanceWriter = Substitute.For<IWriteRepository<LeaveBalance>>();
    private readonly IReadRepository<BlackoutPeriod> _blackouts = Substitute.For<IReadRepository<BlackoutPeriod>>();
    private readonly IReadRepository<Holiday> _holidays = Substitute.For<IReadRepository<Holiday>>();
    private readonly IWriteRepository<ApprovalChain> _chainWriter = Substitute.For<IWriteRepository<ApprovalChain>>();
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();
    private readonly IReadRepository<Role> _roles = Substitute.For<IReadRepository<Role>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public SubmitLeaveRequestCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _blackouts.ListAsync(Arg.Any<BlackoutPeriodsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<BlackoutPeriod>());
        _leaveRequests.ListAsync(Arg.Any<OverlappingLeaveRequestsSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest>());
        _holidays.ListAsync(Arg.Any<HolidaysByLocationAndRangeSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<Holiday>());
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((LeaveBalance?)null);
    }

    private SubmitLeaveRequestCommandHandler CreateHandler() => new(
        _users, _employees, _leaveTypes, _leavePolicies, _leaveRequests, _leaveRequestWriter, _balances, _balanceWriter, _blackouts,
        _holidays, _chainWriter, new LeaveApprovalChainBuilder(_reportingRelationships, _roles, _users), _tenantContext, _currentUser,
        _dateTimeProvider);

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    private Employee CreateEmployee(LocationId locationId) => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
        DepartmentId.New(), DesignationId.New(), locationId, Now, "seed").Value;

    private void SetUpManager(EmployeeId employeeId, EmployeeId managerId) =>
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns(ReportingRelationship.Create(_tenantId, employeeId, managerId, new DateOnly(2020, 1, 1), null).Value);

    private (User CallerUser, Employee Employee, LeaveType LeaveType) SetUpHappyPath(NegativeBalancePolicy negativeBalancePolicy = NegativeBalancePolicy.AllowWithLop)
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(LocationId.New());
        var callerUser = CreateCallerUser(employee.Id);
        var leaveType = LeaveType.Create(_tenantId, "Earned Leave", true, 0, Now, "system").Value;
        var policy = LeavePolicy.Create(_tenantId, leaveType.Id, 12, 1, 5, new DateOnly(2025, 1, 1), null).Value;
        policy.ConfigureBalanceRules(negativeBalancePolicy, 30, false);

        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(callerUser);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);
        _leavePolicies.FirstOrDefaultAsync(Arg.Any<LeavePolicyByLeaveTypeSpecification>(), Arg.Any<CancellationToken>()).Returns(policy);
        SetUpManager(employee.Id, EmployeeId.New());

        return (callerUser, employee, leaveType);
    }

    [Fact]
    public async Task Handle_Should_Submit_A_LeaveRequest_When_Balance_Is_Sufficient()
    {
        var (_, employee, leaveType) = SetUpHappyPath();
        var balance = LeaveBalance.Open(_tenantId, employee.Id, leaveType.Id);
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 10m, "Accrual", Now, "system");
        _balances.FirstOrDefaultAsync(Arg.Any<LeaveBalanceByEmployeeAndTypeSpecification>(), Arg.Any<CancellationToken>()).Returns(balance);

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedDays.Should().Be(2);
        result.Value.LossOfPayDays.Should().Be(0);
        await _leaveRequestWriter.Received(1).AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
        await _chainWriter.Received(1).AddAsync(Arg.Any<ApprovalChain>(), Arg.Any<CancellationToken>());
        _balanceWriter.Received(1).Update(balance);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var command = new SubmitLeaveRequestCommand(Guid.NewGuid(), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Caller_Has_No_Employee_Profile()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(null));

        var command = new SubmitLeaveRequestCommand(Guid.NewGuid(), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.no_employee_profile");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Employee_Not_Found()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(EmployeeId.New()));
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var command = new SubmitLeaveRequestCommand(Guid.NewGuid(), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.employee_not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_LeaveType_Not_Found()
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(LocationId.New());
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employee.Id));
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((LeaveType?)null);

        var command = new SubmitLeaveRequestCommand(Guid.NewGuid(), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.leave_type_not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Period_Is_Invalid()
    {
        var (_, _, leaveType) = SetUpHappyPath();

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 5), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Employee_Gender_Is_Not_Eligible()
    {
        var (_, employee, leaveType) = SetUpHappyPath();
        employee.SetGender(Gender.Male, Now, "system");
        leaveType.UpdateEligibilityRules(Gender.Female, 0, false, 0, Now, "system");

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.gender_not_eligible");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Employee_Has_Not_Met_Minimum_Tenure()
    {
        var (_, _, leaveType) = SetUpHappyPath();
        leaveType.UpdateEligibilityRules(null, 600, false, 0, Now, "system");

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.tenure_not_eligible");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Dates_Fall_Within_A_Blackout_Period()
    {
        var (_, _, leaveType) = SetUpHappyPath();
        var blackout = BlackoutPeriod.Create(
            _tenantId, DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Value, "Year-end freeze", null, Now, "system").Value;
        _blackouts.ListAsync(Arg.Any<BlackoutPeriodsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<BlackoutPeriod> { blackout });

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.blackout_period");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Request_Overlaps_An_Existing_Request()
    {
        var (_, employee, leaveType) = SetUpHappyPath();
        var existing = LeaveRequest.Submit(
            _tenantId, employee.Id, leaveType.Id, DateRange.Create(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6)).Value, 2,
            "Vacation", Now, "system").Value;
        _leaveRequests.ListAsync(Arg.Any<OverlappingLeaveRequestsSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { existing });

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.overlaps_existing");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Active_Policy_Exists()
    {
        var (_, _, leaveType) = SetUpHappyPath();
        _leavePolicies.FirstOrDefaultAsync(Arg.Any<LeavePolicyByLeaveTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((LeavePolicy?)null);

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.no_active_policy");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Requested_Period_Has_Zero_Working_Days()
    {
        var (_, _, leaveType) = SetUpHappyPath();

        // 2026-01-03 and 2026-01-04 are a Saturday and Sunday.
        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 4), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.zero_days");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Active_Manager_Is_Configured()
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(LocationId.New());
        var callerUser = CreateCallerUser(employee.Id);
        var leaveType = LeaveType.Create(_tenantId, "Earned Leave", true, 0, Now, "system").Value;
        var policy = LeavePolicy.Create(_tenantId, leaveType.Id, 12, 1, 5, new DateOnly(2025, 1, 1), null).Value;

        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(callerUser);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);
        _leaveTypes.FirstOrDefaultAsync(Arg.Any<LeaveTypeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(leaveType);
        _leavePolicies.FirstOrDefaultAsync(Arg.Any<LeavePolicyByLeaveTypeSpecification>(), Arg.Any<CancellationToken>()).Returns(policy);
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveManagerRelationshipSpecification>(), Arg.Any<CancellationToken>())
            .Returns((ReportingRelationship?)null);

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.no_manager");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Balance_Is_Insufficient_And_Policy_Does_Not_Allow_LossOfPay()
    {
        var (_, _, leaveType) = SetUpHappyPath(NegativeBalancePolicy.NotAllowed);

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.insufficient_balance");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_LossOfPay_Would_Apply_But_Not_Acknowledged()
    {
        var (_, _, leaveType) = SetUpHappyPath(NegativeBalancePolicy.AllowWithLop);

        var command = new SubmitLeaveRequestCommand(
            leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", AcknowledgeInsufficientBalance: false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.insufficient_balance_requires_ack");
    }

    [Fact]
    public async Task Handle_Should_Route_Uncovered_Days_To_LossOfPay_When_Acknowledged()
    {
        var (_, _, leaveType) = SetUpHappyPath(NegativeBalancePolicy.AllowWithLop);

        var command = new SubmitLeaveRequestCommand(
            leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", AcknowledgeInsufficientBalance: true);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LossOfPayDays.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_Allow_Negative_Balance_Without_LossOfPay_When_Policy_Allows_It()
    {
        var (_, _, leaveType) = SetUpHappyPath(NegativeBalancePolicy.AllowNegative);

        var command = new SubmitLeaveRequestCommand(leaveType.Id.Value, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Vacation", false);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LossOfPayDays.Should().Be(0);
        await _leaveRequestWriter.Received(1).AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
    }
}
