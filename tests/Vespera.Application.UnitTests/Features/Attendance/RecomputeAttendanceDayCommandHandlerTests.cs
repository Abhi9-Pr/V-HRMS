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

public class RecomputeAttendanceDayCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly IReadRepository<AttendanceDay> _attendanceDays = Substitute.For<IReadRepository<AttendanceDay>>();
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter = Substitute.For<IWriteRepository<AttendanceDay>>();
    private readonly IReadRepository<ShiftRoster> _rosters = Substitute.For<IReadRepository<ShiftRoster>>();
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly IReadRepository<Domain.Attendance.Holiday> _holidays = Substitute.For<IReadRepository<Domain.Attendance.Holiday>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ITimeZoneConverter _timeZoneConverter = Substitute.For<ITimeZoneConverter>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 16, 3, 0, 0, TimeSpan.Zero);
    private static readonly GeoCoordinate SiteCoordinate = GeoCoordinate.Create(12.9716, 77.5946).Value;

    public RecomputeAttendanceDayCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _rosters.ListAsync(Arg.Any<ISpecification<ShiftRoster>>(), Arg.Any<CancellationToken>()).Returns([]);
        _holidays.FirstOrDefaultAsync(Arg.Any<ISpecification<Domain.Attendance.Holiday>>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Attendance.Holiday?)null);
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>())
            .Returns((AttendanceDay?)null);
    }

    private RecomputeAttendanceDayCommandHandler CreateHandler() => new(
        _employees, _locations, _attendanceDays, _attendanceDayWriter, _rosters, _shifts, _holidays, _tenantContext, _dateTimeProvider, _timeZoneConverter);

    private Employee CreateEmployee(LocationId locationId) => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-600").Value, "Grace", "Hopper",
        EmailAddress.Create("grace@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
        DepartmentId.New(), DesignationId.New(), locationId, Now, "seed").Value;

    private Location CreateLocation() => Location.Create(
        _tenantId, "HQ", "1 Main St", "City", "Country", SiteCoordinate, "Asia/Kolkata", Now, "seed").Value;

    private (Employee Employee, Location Location) SetUpEmployeeAndLocation()
    {
        var location = CreateLocation();
        var employee = CreateEmployee(location.Id);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns(location);
        _timeZoneConverter.ToZoned(Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(call => NodaTimeZoneConverterProbe.ToZoned(call.ArgAt<DateTimeOffset>(0), call.ArgAt<string>(1)));
        return (employee, location);
    }

    [Fact]
    public async Task Handle_Should_Create_An_AttendanceDay_And_Mark_It_Absent_When_No_Punches_Exist_But_A_Shift_Is_Assigned()
    {
        var (employee, _) = SetUpEmployeeAndLocation();
        var shift = Shift.Create(_tenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, Now, "seed").Value;
        var roster = ShiftRoster.Create(
            _tenantId, employee.Id, shift.Id, DateRange.Create(new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15)).Value, Now);
        roster.Publish(Now, "hr@vespera.test");
        _rosters.ListAsync(Arg.Any<ISpecification<ShiftRoster>>(), Arg.Any<CancellationToken>()).Returns([roster]);
        _shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);

        var handler = CreateHandler();
        var command = new RecomputeAttendanceDayCommand(employee.Id.Value, new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attendanceDayWriter.Received(1).AddAsync(
            Arg.Is<AttendanceDay>(d => d.Status == AttendanceDayStatus.Absent && d.IsLopCandidate), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Update_An_Existing_Day_Rather_Than_Recreating_It()
    {
        var (employee, _) = SetUpEmployeeAndLocation();
        var existingDay = AttendanceDay.Open(_tenantId, employee.Id, new DateOnly(2026, 1, 15));
        existingDay.RecordPunch(PunchType.In, new DateTimeOffset(2026, 1, 15, 3, 30, 0, TimeSpan.Zero), null, PunchSource.Web);
        existingDay.RecordPunch(PunchType.Out, new DateTimeOffset(2026, 1, 15, 12, 30, 0, TimeSpan.Zero), null, PunchSource.Web);
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>()).Returns(existingDay);

        var handler = CreateHandler();
        var command = new RecomputeAttendanceDayCommand(employee.Id.Value, new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _attendanceDayWriter.Received(1).Update(existingDay);
        await _attendanceDayWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        existingDay.WorkedMinutes.Should().Be(9 * 60);
    }

    [Fact]
    public async Task Handle_Should_Not_Flag_LopCandidate_On_A_Holiday_Even_With_No_Punches()
    {
        var (employee, location) = SetUpEmployeeAndLocation();
        var holiday = Domain.Attendance.Holiday.Create(_tenantId, location.Id, new DateOnly(2026, 1, 15), "New Year", Now, "hr@vespera.test").Value;
        _holidays.FirstOrDefaultAsync(Arg.Any<ISpecification<Domain.Attendance.Holiday>>(), Arg.Any<CancellationToken>()).Returns(holiday);

        var handler = CreateHandler();
        var command = new RecomputeAttendanceDayCommand(employee.Id.Value, new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _attendanceDayWriter.Received(1).AddAsync(
            Arg.Is<AttendanceDay>(d => d.Status == AttendanceDayStatus.Holiday && !d.IsLopCandidate), Arg.Any<CancellationToken>());
    }

    /// <summary>A minimal stand-in used only to produce a real <c>ZonedDateTime</c> from a UTC
    /// instant for the mocked <see cref="ITimeZoneConverter"/> above — delegates to the real
    /// NodaTime provider rather than reimplementing conversion math in the test.</summary>
    private sealed class NodaTimeZoneConverterProbe
    {
        public static NodaTime.ZonedDateTime ToZoned(DateTimeOffset utc, string timeZoneId)
        {
            var zone = NodaTime.DateTimeZoneProviders.Tzdb[timeZoneId];
            return NodaTime.Instant.FromDateTimeOffset(utc).InZone(zone);
        }
    }
}
