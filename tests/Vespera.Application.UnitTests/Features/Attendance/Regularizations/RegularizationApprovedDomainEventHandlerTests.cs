using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Domain.Attendance;
using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance.Regularizations;

public class RegularizationApprovedDomainEventHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 16, 3, 0, 0, TimeSpan.Zero);
    private static readonly GeoCoordinate SiteCoordinate = GeoCoordinate.Create(12.9716, 77.5946).Value;

    private readonly IReadRepositoryAdmin<AttendanceDay> _attendanceDaysAdmin = Substitute.For<IReadRepositoryAdmin<AttendanceDay>>();
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter = Substitute.For<IWriteRepository<AttendanceDay>>();
    private readonly IReadRepositoryAdmin<Employee> _employeesAdmin = Substitute.For<IReadRepositoryAdmin<Employee>>();
    private readonly IReadRepositoryAdmin<Location> _locationsAdmin = Substitute.For<IReadRepositoryAdmin<Location>>();
    private readonly IReadRepositoryAdmin<ShiftRoster> _rostersAdmin = Substitute.For<IReadRepositoryAdmin<ShiftRoster>>();
    private readonly IReadRepositoryAdmin<Shift> _shiftsAdmin = Substitute.For<IReadRepositoryAdmin<Shift>>();
    private readonly IReadRepositoryAdmin<Holiday> _holidaysAdmin = Substitute.For<IReadRepositoryAdmin<Holiday>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ITimeZoneConverter _timeZoneConverter = Substitute.For<ITimeZoneConverter>();

    public RegularizationApprovedDomainEventHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _rostersAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ShiftRoster>>(), Arg.Any<CancellationToken>()).Returns([]);
        _holidaysAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<Holiday>>(), Arg.Any<CancellationToken>()).Returns([]);
        _timeZoneConverter.ToZoned(Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(call => NodaTimeZoneConverterProbe.ToZoned(call.ArgAt<DateTimeOffset>(0), call.ArgAt<string>(1)));
    }

    private RegularizationApprovedDomainEventHandler CreateHandler() => new(
        _attendanceDaysAdmin, _attendanceDayWriter, _employeesAdmin, _locationsAdmin, _rostersAdmin, _shiftsAdmin, _holidaysAdmin,
        _dateTimeProvider, _timeZoneConverter);

    private static Employee CreateEmployee(LocationId locationId) => Employee.Onboard(
        TenantId, EmployeeCode.Create("EMP-700").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
        DepartmentId.New(), DesignationId.New(), locationId, Now, "seed").Value;

    private static Location CreateLocation() =>
        Location.Create(TenantId, "HQ", "1 Main St", "City", "Country", SiteCoordinate, "Asia/Kolkata", Now, "seed").Value;

    [Fact]
    public async Task Handle_Should_Recompute_And_Persist_The_Attendance_Day()
    {
        var location = CreateLocation();
        var employee = CreateEmployee(location.Id);
        var day = AttendanceDay.Open(TenantId, employee.Id, new DateOnly(2026, 1, 15));
        day.RecordPunch(PunchType.In, new DateTimeOffset(2026, 1, 15, 3, 30, 0, TimeSpan.Zero), null, PunchSource.Web);
        day.RecordPunch(PunchType.Out, new DateTimeOffset(2026, 1, 15, 12, 30, 0, TimeSpan.Zero), null, PunchSource.Web);

        _attendanceDaysAdmin.ListIgnoringFiltersAsync(Arg.Any<AttendanceDayByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([day]);
        _employeesAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns([employee]);
        _locationsAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>()).Returns([location]);

        var notification = new DomainEventNotification<RegularizationApproved>(
            new RegularizationApproved(RegularizationRequestId.New(), TenantId, employee.Id, day.Id, Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        _attendanceDayWriter.Received(1).Update(day);
        day.WorkedMinutes.Should().Be(9 * 60);
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Attendance_Day_Cannot_Be_Found()
    {
        _attendanceDaysAdmin.ListIgnoringFiltersAsync(Arg.Any<AttendanceDayByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var notification = new DomainEventNotification<RegularizationApproved>(
            new RegularizationApproved(RegularizationRequestId.New(), TenantId, EmployeeId.New(), AttendanceDayId.New(), Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        _attendanceDayWriter.DidNotReceive().Update(Arg.Any<AttendanceDay>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Employee_Cannot_Be_Found()
    {
        var day = AttendanceDay.Open(TenantId, EmployeeId.New(), new DateOnly(2026, 1, 15));
        _attendanceDaysAdmin.ListIgnoringFiltersAsync(Arg.Any<AttendanceDayByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([day]);
        _employeesAdmin.ListIgnoringFiltersAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns([]);

        var notification = new DomainEventNotification<RegularizationApproved>(
            new RegularizationApproved(RegularizationRequestId.New(), TenantId, day.EmployeeId, day.Id, Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        _attendanceDayWriter.DidNotReceive().Update(Arg.Any<AttendanceDay>());
    }

    /// <summary>A minimal stand-in used only to produce a real <c>ZonedDateTime</c> from a UTC
    /// instant for the mocked <see cref="ITimeZoneConverter"/> above.</summary>
    private sealed class NodaTimeZoneConverterProbe
    {
        public static NodaTime.ZonedDateTime ToZoned(DateTimeOffset utc, string timeZoneId)
        {
            var zone = NodaTime.DateTimeZoneProviders.Tzdb[timeZoneId];
            return NodaTime.Instant.FromDateTimeOffset(utc).InZone(zone);
        }
    }
}
