using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class ResolveQuarantinedPunchCommandHandlerTests
{
    private readonly IReadRepository<QuarantinedBiometricPunch> _quarantinedPunches = Substitute.For<IReadRepository<QuarantinedBiometricPunch>>();
    private readonly IWriteRepository<QuarantinedBiometricPunch> _quarantinedPunchWriter = Substitute.For<IWriteRepository<QuarantinedBiometricPunch>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IWriteRepository<Employee> _employeeWriter = Substitute.For<IWriteRepository<Employee>>();
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly IReadRepository<AttendanceDay> _attendanceDays = Substitute.For<IReadRepository<AttendanceDay>>();
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter = Substitute.For<IWriteRepository<AttendanceDay>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ITimeZoneConverter _timeZoneConverter = Substitute.For<ITimeZoneConverter>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly TenantId _tenantId = TenantId.New();
    private readonly DateTimeOffset _now = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);

    public ResolveQuarantinedPunchCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(_now);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _timeZoneConverter.ResolveLocalDate(Arg.Any<DateTimeOffset>(), Arg.Any<string>()).Returns(new DateOnly(2026, 2, 1));
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>()).Returns((AttendanceDay?)null);
    }

    private ResolveQuarantinedPunchCommandHandler CreateHandler() => new(
        _quarantinedPunches, _quarantinedPunchWriter, _employees, _employeeWriter, _locations,
        _attendanceDays, _attendanceDayWriter, _tenantContext, _dateTimeProvider, _timeZoneConverter, _currentUser);

    private Employee CreateEmployee(LocationId locationId) => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-700").Value, "Alan", "Turing",
        EmailAddress.Create("alan@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), DepartmentId.New(), DesignationId.New(), locationId, _now, "seed").Value;

    private Location CreateLocation() => Location.Create(
        _tenantId, "HQ", "1 Main St", "City", "Country", GeoCoordinate.Create(12.9716, 77.5946).Value, "Asia/Kolkata", _now, "seed").Value;

    [Fact]
    public async Task Handle_Should_Resolve_The_Entry_And_Create_A_Real_Punch_On_Success()
    {
        var device = BiometricDevice.Register(_tenantId, LocationId.New(), BiometricVendorType.ZKTeco, "device.local", 4370, null, _now, "seed").Value;
        var entry = QuarantinedBiometricPunch.Create(_tenantId, device.Id, "ZK-042", _now, PunchType.In, "rec-1");
        var location = CreateLocation();
        var employee = CreateEmployee(location.Id);

        _quarantinedPunches.FirstOrDefaultAsync(Arg.Any<ISpecification<QuarantinedBiometricPunch>>(), Arg.Any<CancellationToken>()).Returns(entry);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveQuarantinedPunchCommand(entry.Id.Value, employee.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(QuarantinedBiometricPunchStatus.Resolved);
        entry.ResolvedEmployeeId.Should().Be(employee.Id);
        await _attendanceDayWriter.Received(1).AddAsync(
            Arg.Is<AttendanceDay>(d => d.Punches.Count == 1 && d.Punches.Single().Source == PunchSource.Biometric), Arg.Any<CancellationToken>());
        _quarantinedPunchWriter.Received(1).Update(entry);
        employee.BiometricDeviceUserId.Should().Be("ZK-042");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Entry_Is_Already_Resolved()
    {
        var device = BiometricDevice.Register(_tenantId, LocationId.New(), BiometricVendorType.ZKTeco, "device.local", 4370, null, _now, "seed").Value;
        var entry = QuarantinedBiometricPunch.Create(_tenantId, device.Id, "ZK-042", _now, PunchType.In, "rec-1");
        entry.Resolve(EmployeeId.New());
        var location = CreateLocation();
        var employee = CreateEmployee(location.Id);

        _quarantinedPunches.FirstOrDefaultAsync(Arg.Any<ISpecification<QuarantinedBiometricPunch>>(), Arg.Any<CancellationToken>()).Returns(entry);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveQuarantinedPunchCommand(entry.Id.Value, employee.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("quarantined_biometric_punch.already_resolved");
        await _attendanceDayWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Employee_Is_Not_Found()
    {
        var device = BiometricDevice.Register(_tenantId, LocationId.New(), BiometricVendorType.ZKTeco, "device.local", 4370, null, _now, "seed").Value;
        var entry = QuarantinedBiometricPunch.Create(_tenantId, device.Id, "ZK-042", _now, PunchType.In, "rec-1");

        _quarantinedPunches.FirstOrDefaultAsync(Arg.Any<ISpecification<QuarantinedBiometricPunch>>(), Arg.Any<CancellationToken>()).Returns(entry);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveQuarantinedPunchCommand(entry.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }
}
