using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Departments;
using Vespera.Application.Features.Designations;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Holidays;
using Vespera.Application.Features.Leave;
using Vespera.Application.Features.Locations;
using Vespera.Application.Features.Mobile;
using Vespera.Application.Features.Rosters;
using EmployeeByIdSpecification = Vespera.Application.Features.Employees.EmployeeByIdSpecification;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Mobile;

public class GetMobileBootstrapQueryHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<GeofenceZone> _geofenceZones = Substitute.For<IReadRepository<GeofenceZone>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetMobileBootstrapQueryHandler CreateHandler() => new(
        _sender, _employees, _geofenceZones, _tenantContext, _dateTimeProvider,
        new CurrentEmployeeResolver(_users, _currentUser));

    [Fact]
    public async Task Handle_Should_Fail_When_Account_Has_No_Linked_Employee()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new GetMobileBootstrapQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("mobile_bootstrap.no_linked_employee");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Linked_Employee_Record_Is_Missing()
    {
        var employeeId = EmployeeId.New();
        _currentUser.UserId.Returns(Guid.NewGuid());
        var user = User.Create(_tenantId, EmailAddress.Create("user@demo.vespera.test").Value, employeeId, DateTimeOffset.UtcNow, "system");
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await CreateHandler().Handle(new GetMobileBootstrapQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("mobile_bootstrap.employee_not_found");
    }

    [Fact]
    public async Task Handle_Should_Compose_Profile_Geofences_Shifts_And_Policy_Versions()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _dateTimeProvider.UtcNow.Returns(now);

        var employeeId = EmployeeId.New();
        _currentUser.UserId.Returns(Guid.NewGuid());
        var user = User.Create(_tenantId, EmailAddress.Create("user@demo.vespera.test").Value, employeeId, now, "system");
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);

        var employee = Employee.Onboard(
            _tenantId, EmployeeCode.Create("EMP-001").Value, "Priya", "Sharma",
            EmailAddress.Create("priya.sharma@demo.vespera.test").Value, PhoneNumber.Create("+919876543210").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            now, "system").Value;
        _tenantContext.TenantId.Returns(_tenantId);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);

        _sender.Send(Arg.Any<GetRosterQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new RosterResultDto([new EmployeeRosterDto(employee.Id.Value, "Priya Sharma", [new RosterDayDto(new DateOnly(2026, 1, 1), null, null)])])));
        _geofenceZones.ListAsync(Arg.Any<GeofenceZoneByLocationIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns([GeofenceZone.Create(_tenantId, employee.LocationId, "Head Office", GeoCoordinate.Create(12.9, 77.5).Value, 100, now, "system").Value]);

        _sender.Send(Arg.Any<GetDepartmentsETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("dept-etag"));
        _sender.Send(Arg.Any<GetDesignationsETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("desig-etag"));
        _sender.Send(Arg.Any<GetLocationsETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("loc-etag"));
        _sender.Send(Arg.Any<GetLeaveTypesETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("leave-etag"));
        _sender.Send(Arg.Any<GetHolidaysETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("holiday-etag"));

        var result = await CreateHandler().Handle(new GetMobileBootstrapQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Profile.EmployeeId.Should().Be(employee.Id.Value);
        result.Value.Profile.FirstName.Should().Be("Priya");
        result.Value.UpcomingShifts.Should().ContainSingle();
        result.Value.Geofences.Should().ContainSingle(g => g.Name == "Head Office");
        result.Value.PolicyVersions.Departments.Should().Be("dept-etag");
        result.Value.PolicyVersions.Holidays.Should().Be("holiday-etag");
    }

    [Fact]
    public async Task Handle_Should_Default_ETags_To_Empty_String_When_The_Underlying_Query_Fails()
    {
        var now = DateTimeOffset.UtcNow;
        _dateTimeProvider.UtcNow.Returns(now);
        var employeeId = EmployeeId.New();
        _currentUser.UserId.Returns(Guid.NewGuid());
        var user = User.Create(_tenantId, EmailAddress.Create("user@demo.vespera.test").Value, employeeId, now, "system");
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
        var employee = Employee.Onboard(
            _tenantId, EmployeeCode.Create("EMP-002").Value, "Amit", "Rao",
            EmailAddress.Create("amit.rao@demo.vespera.test").Value, PhoneNumber.Create("+919876500000").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            now, "system").Value;
        _tenantContext.TenantId.Returns(_tenantId);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);
        _sender.Send(Arg.Any<GetRosterQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RosterResultDto>(Error.Failure("roster.error", "boom")));
        _geofenceZones.ListAsync(Arg.Any<GeofenceZoneByLocationIdSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
        _sender.Send(Arg.Any<GetDepartmentsETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<string>(Error.Failure("etag.error", "boom")));
        _sender.Send(Arg.Any<GetDesignationsETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("desig-etag"));
        _sender.Send(Arg.Any<GetLocationsETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("loc-etag"));
        _sender.Send(Arg.Any<GetLeaveTypesETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("leave-etag"));
        _sender.Send(Arg.Any<GetHolidaysETagQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success("holiday-etag"));

        var result = await CreateHandler().Handle(new GetMobileBootstrapQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpcomingShifts.Should().BeEmpty();
        result.Value.PolicyVersions.Departments.Should().BeEmpty();
    }
}
