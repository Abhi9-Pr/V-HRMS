using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Leave;
using Vespera.Application.Features.OrgChart;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class GetTeamLeaveCalendarQueryHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private GetTeamLeaveCalendarQueryHandler CreateHandler() => new(_users, _employees, _leaveRequests, _tenantContext, _currentUser);

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    private Employee CreateEmployee(DepartmentId departmentId, string code) => Employee.Onboard(
        _tenantId, EmployeeCode.Create(code).Value, "Grace", "Hopper",
        EmailAddress.Create($"{code}@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
        departmentId, DesignationId.New(), LocationId.New(), Now, "seed").Value;

    private LeaveRequest CreateApprovedLeaveRequest(EmployeeId employeeId, DateOnly from, DateOnly to)
    {
        var leaveRequest = LeaveRequest.Submit(
            _tenantId, employeeId, LeaveTypeId.New(), DateRange.Create(from, to).Value, 2, "Vacation", Now, "system").Value;
        leaveRequest.Approve(EmployeeId.New(), Now, "manager");
        return leaveRequest;
    }

    [Fact]
    public async Task Handle_Should_Return_A_Day_Per_Date_In_Range_With_Employees_On_Leave()
    {
        var departmentId = DepartmentId.New();
        var employeeOnLeave = CreateEmployee(departmentId, "EMP-1");
        var employeeNotOnLeave = CreateEmployee(departmentId, "EMP-2");
        _tenantContext.TenantId.Returns(_tenantId);
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { employeeOnLeave, employeeNotOnLeave });
        var approvedRequest = CreateApprovedLeaveRequest(employeeOnLeave.Id, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 3));
        _leaveRequests.ListAsync(Arg.Any<ApprovedLeaveRequestsOverlappingRangeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { approvedRequest });

        var query = new GetTeamLeaveCalendarQuery(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 3), departmentId.Value);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.First(d => d.Date == new DateOnly(2026, 2, 1)).EmployeeIdsOnLeave.Should().BeEmpty();
        var conflictDay = result.Value.First(d => d.Date == new DateOnly(2026, 2, 2));
        conflictDay.EmployeeIdsOnLeave.Should().ContainSingle(id => id == employeeOnLeave.Id.Value);
        conflictDay.ConflictWarning.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Default_To_The_Callers_Own_Department_When_None_Is_Given()
    {
        var departmentId = DepartmentId.New();
        var employee = CreateEmployee(departmentId, "EMP-3");
        var userId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(employee.Id));
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { employee });
        _leaveRequests.ListAsync(Arg.Any<ApprovedLeaveRequestsOverlappingRangeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest>());

        var query = new GetTeamLeaveCalendarQuery(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1), null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_DepartmentId_Given_And_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetTeamLeaveCalendarQuery(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1), null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_calendar.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_Caller_Has_No_Employee_Profile()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(null));

        var query = new GetTeamLeaveCalendarQuery(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1), null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_The_Department_Has_No_Employees()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<Employee>());

        var query = new GetTeamLeaveCalendarQuery(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1), Guid.NewGuid());
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
