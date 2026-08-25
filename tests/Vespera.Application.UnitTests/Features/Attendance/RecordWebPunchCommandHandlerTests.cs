using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class RecordWebPunchCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly IReadRepository<GeofenceZone> _geofenceZones = Substitute.For<IReadRepository<GeofenceZone>>();
    private readonly IReadRepository<AttendanceDay> _attendanceDays = Substitute.For<IReadRepository<AttendanceDay>>();
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter = Substitute.For<IWriteRepository<AttendanceDay>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ITimeZoneConverter _timeZoneConverter = Substitute.For<ITimeZoneConverter>();
    private readonly IDistanceCalculator _distanceCalculator = new HaversineDistanceCalculator();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly DateTimeOffset _now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);
    private readonly DateOnly _today = new(2026, 3, 10);

    public RecordWebPunchCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(_now);
        _timeZoneConverter.ResolveLocalDate(Arg.Any<DateTimeOffset>(), Arg.Any<string>()).Returns(_today);

        // No attendance day exists yet by default; RecordPunch opens a new one.
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>())
            .Returns((AttendanceDay?)null);
    }

    private RecordWebPunchCommandHandler CreateHandler() => new(
        _employees, _locations, _geofenceZones, _attendanceDays, _attendanceDayWriter,
        _tenantContext, _dateTimeProvider, _timeZoneConverter, _distanceCalculator);

    private Employee CreateEmployee(LocationId locationId) => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-200").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), locationId, _now, "seed").Value;

    private Location CreateLocation(GeoCoordinate coordinate) => Location.Create(
        _tenantId, "HQ", "1 Main St", "City", "Country", coordinate, "Asia/Kolkata", _now, "seed").Value;

    private void SetUpEmployeeAndLocation(Employee employee, Location location)
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_Punch_Is_Inside_The_Geofence()
    {
        var siteCoordinate = GeoCoordinate.Create(12.9716, 77.5946).Value;
        var location = CreateLocation(siteCoordinate);
        var employee = CreateEmployee(location.Id);
        SetUpEmployeeAndLocation(employee, location);

        var zone = GeofenceZone.Create(_tenantId, location.Id, "HQ Fence", siteCoordinate, 200, _now, "seed").Value;
        _geofenceZones.FirstOrDefaultAsync(Arg.Any<ISpecification<GeofenceZone>>(), Arg.Any<CancellationToken>()).Returns(zone);

        var handler = CreateHandler();
        // A few metres away — well inside a 200m radius.
        var command = new RecordWebPunchCommand(employee.Id.Value, "In", 12.9717, 77.5947);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attendanceDayWriter.Received(1).AddAsync(Arg.Is<AttendanceDay>(d => d.Punches.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Reject_When_Punch_Is_Outside_The_Geofence()
    {
        var siteCoordinate = GeoCoordinate.Create(12.9716, 77.5946).Value;
        var location = CreateLocation(siteCoordinate);
        var employee = CreateEmployee(location.Id);
        SetUpEmployeeAndLocation(employee, location);

        var zone = GeofenceZone.Create(_tenantId, location.Id, "HQ Fence", siteCoordinate, 100, _now, "seed").Value;
        _geofenceZones.FirstOrDefaultAsync(Arg.Any<ISpecification<GeofenceZone>>(), Arg.Any<CancellationToken>()).Returns(zone);

        var handler = CreateHandler();
        // Roughly 11km away — far outside a 100m radius.
        var command = new RecordWebPunchCommand(employee.Id.Value, "In", 13.0716, 77.5946);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("attendance.punch.outside_geofence");
        result.Error.Message.Should().ContainAny("100m");
        await _attendanceDayWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Succeed_Unconditionally_When_No_Geofence_Is_Configured()
    {
        var location = CreateLocation(GeoCoordinate.Create(12.9716, 77.5946).Value);
        var employee = CreateEmployee(location.Id);
        SetUpEmployeeAndLocation(employee, location);

        _geofenceZones.FirstOrDefaultAsync(Arg.Any<ISpecification<GeofenceZone>>(), Arg.Any<CancellationToken>())
            .Returns((GeofenceZone?)null);

        var handler = CreateHandler();
        // Nowhere near the site — irrelevant, since no geofence is configured for this location.
        var command = new RecordWebPunchCommand(employee.Id.Value, "In", 40.7128, -74.0060);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Succeed_Without_Coordinates()
    {
        var location = CreateLocation(GeoCoordinate.Create(12.9716, 77.5946).Value);
        var employee = CreateEmployee(location.Id);
        SetUpEmployeeAndLocation(employee, location);

        var handler = CreateHandler();
        var command = new RecordWebPunchCommand(employee.Id.Value, "In", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _geofenceZones.DidNotReceiveWithAnyArgs().FirstOrDefaultAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var handler = CreateHandler();
        var command = new RecordWebPunchCommand(Guid.NewGuid(), "In", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }

    [Fact]
    public async Task Handle_Should_Enforce_Alternating_In_Out_Via_The_Domain_Rule()
    {
        var location = CreateLocation(GeoCoordinate.Create(12.9716, 77.5946).Value);
        var employee = CreateEmployee(location.Id);
        SetUpEmployeeAndLocation(employee, location);

        var existingDay = AttendanceDay.Open(_tenantId, employee.Id, _today);
        existingDay.RecordPunch(PunchType.In, _now.AddHours(-1), null, PunchSource.Web);
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>())
            .Returns(existingDay);

        var handler = CreateHandler();
        // Another "In" while already punched in — the domain rule rejects it.
        var command = new RecordWebPunchCommand(employee.Id.Value, "In", null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("attendance_day.consecutive_same_punch");
        // The day already existed — never re-added, only mutated in place.
        await _attendanceDayWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
