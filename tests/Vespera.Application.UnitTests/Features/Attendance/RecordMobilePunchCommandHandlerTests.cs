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

public class RecordMobilePunchCommandHandlerTests
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
    private static readonly GeoCoordinate SiteCoordinate = GeoCoordinate.Create(12.9716, 77.5946).Value;

    public RecordMobilePunchCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(_now);
        _timeZoneConverter.ResolveLocalDate(Arg.Any<DateTimeOffset>(), Arg.Any<string>()).Returns(_today);

        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>())
            .Returns((AttendanceDay?)null);
        _attendanceDays.ListAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private RecordMobilePunchCommandHandler CreateHandler() => new(
        _employees, _locations, _geofenceZones, _attendanceDays, _attendanceDayWriter,
        _tenantContext, _dateTimeProvider, _timeZoneConverter, _distanceCalculator);

    private Employee CreateEmployee(LocationId locationId) => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-201").Value, "Grace", "Hopper",
        EmailAddress.Create("grace@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), locationId, _now, "seed").Value;

    private Location CreateLocation() => Location.Create(
        _tenantId, "HQ", "1 Main St", "City", "Country", SiteCoordinate, "Asia/Kolkata", _now, "seed").Value;

    private (Employee Employee, Location Location) SetUpEmployeeAndLocation()
    {
        var location = CreateLocation();
        var employee = CreateEmployee(location.Id);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);
        _geofenceZones.FirstOrDefaultAsync(Arg.Any<ISpecification<GeofenceZone>>(), Arg.Any<CancellationToken>())
            .Returns((GeofenceZone?)null);
        return (employee, location);
    }

    private static RecordMobilePunchCommand CreateCommand(
        Guid employeeId,
        double? latitude,
        double? longitude,
        double accuracy = 10,
        bool isFromMockProvider = false,
        bool rejectMockProvider = true,
        double maxPlausibleSpeedKmh = 250,
        double minAcceptableAccuracyMetres = 100) =>
        new(employeeId, "In", latitude, longitude, accuracy, isFromMockProvider, "device-1",
            rejectMockProvider, maxPlausibleSpeedKmh, minAcceptableAccuracyMetres, "idem-key-1");

    [Fact]
    public async Task Handle_Should_Succeed_Unflagged_When_The_Clean_Baseline_Case_Applies()
    {
        var (employee, _) = SetUpEmployeeAndLocation();
        var handler = CreateHandler();

        var command = CreateCommand(employee.Id.Value, SiteCoordinate.Latitude + 0.0001, SiteCoordinate.Longitude + 0.0001, accuracy: 5);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attendanceDayWriter.Received(1).AddAsync(
            Arg.Is<AttendanceDay>(d => !d.Punches.Single().RequiresApproval && d.Punches.Single().FlagReason == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Reject_When_Outside_The_Geofence()
    {
        var (employee, location) = SetUpEmployeeAndLocation();
        var zone = GeofenceZone.Create(_tenantId, location.Id, "HQ Fence", SiteCoordinate, 100, _now, "seed").Value;
        _geofenceZones.FirstOrDefaultAsync(Arg.Any<ISpecification<GeofenceZone>>(), Arg.Any<CancellationToken>()).Returns(zone);
        var handler = CreateHandler();

        var command = CreateCommand(employee.Id.Value, SiteCoordinate.Latitude + 0.1, SiteCoordinate.Longitude);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("attendance.punch.outside_geofence");
        await _attendanceDayWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Reject_When_Mock_Provider_Is_Reported_And_Reject_Policy_Enabled()
    {
        var (employee, _) = SetUpEmployeeAndLocation();
        var handler = CreateHandler();

        var command = CreateCommand(employee.Id.Value, null, null, isFromMockProvider: true, rejectMockProvider: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("attendance.punch.mock_location_detected");
        await _attendanceDayWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Flag_Not_Reject_When_Mock_Provider_Is_Reported_But_Reject_Policy_Disabled()
    {
        var (employee, _) = SetUpEmployeeAndLocation();
        var handler = CreateHandler();

        var command = CreateCommand(employee.Id.Value, null, null, isFromMockProvider: true, rejectMockProvider: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attendanceDayWriter.Received(1).AddAsync(
            Arg.Is<AttendanceDay>(d => d.Punches.Single().RequiresApproval && d.Punches.Single().FlagReason!.Contains("mock_location_provider")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Flag_Impossible_Travel_When_The_Previous_Punch_Implies_An_Implausible_Speed()
    {
        var (employee, location) = SetUpEmployeeAndLocation();

        // A punch 2 minutes ago, ~300km away — implies a wildly implausible speed. Must be an
        // "In" punch — it's the first punch of that day, and the domain rejects (silently, if the
        // Result isn't checked) any other first punch type.
        var priorDay = AttendanceDay.Open(_tenantId, employee.Id, _today.AddDays(-1));
        var farAway = GeoCoordinate.Create(SiteCoordinate.Latitude + 3, SiteCoordinate.Longitude).Value;
        var seedResult = priorDay.RecordPunch(PunchType.In, _now.AddMinutes(-2), farAway, PunchSource.Mobile);
        seedResult.IsSuccess.Should().BeTrue();
        _attendanceDays.ListAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>())
            .Returns([priorDay]);

        var handler = CreateHandler();
        var command = CreateCommand(employee.Id.Value, SiteCoordinate.Latitude, SiteCoordinate.Longitude);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attendanceDayWriter.Received(1).AddAsync(
            Arg.Is<AttendanceDay>(d => d.Punches.Single().RequiresApproval && d.Punches.Single().FlagReason!.Contains("impossible_travel")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Not_Flag_Impossible_Travel_When_There_Is_No_Prior_Punch()
    {
        var (employee, _) = SetUpEmployeeAndLocation();
        var handler = CreateHandler();

        var command = CreateCommand(employee.Id.Value, SiteCoordinate.Latitude, SiteCoordinate.Longitude);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attendanceDayWriter.Received(1).AddAsync(
            Arg.Is<AttendanceDay>(d => d.Punches.Single().FlagReason == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Flag_Low_Accuracy()
    {
        var (employee, _) = SetUpEmployeeAndLocation();
        var handler = CreateHandler();

        var command = CreateCommand(employee.Id.Value, SiteCoordinate.Latitude, SiteCoordinate.Longitude, accuracy: 500, minAcceptableAccuracyMetres: 100);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attendanceDayWriter.Received(1).AddAsync(
            Arg.Is<AttendanceDay>(d => d.Punches.Single().RequiresApproval && d.Punches.Single().FlagReason!.Contains("low_gps_accuracy")),
            Arg.Any<CancellationToken>());
    }
}
