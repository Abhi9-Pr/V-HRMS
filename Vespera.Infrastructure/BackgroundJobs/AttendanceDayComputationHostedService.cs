using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Application.Features.Locations;
using Vespera.Application.Features.Rosters;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>
/// Nightly recompute of every active employee's *previous* local day — "previous" resolved per
/// employee, since a tenant spanning multiple locations/timezones has a different "yesterday"
/// boundary per location. Reads cross-tenant via <c>IReadRepositoryAdmin</c>, same shape as
/// <see cref="RetentionPurgeHostedService"/>/<see cref="OffboardingAccessRevocationHostedService"/>.
/// <para>
/// Deliberately does NOT go through <c>RecomputeAttendanceDayCommandHandler</c>/MediatR — that
/// handler resolves the tenant from ambient <c>ITenantContext</c> (populated from an authenticated
/// request's claims), which doesn't exist inside a hosted service's own DI scope. Instead this
/// reuses the same per-entity specifications the handler uses (<see cref="LocationByIdSpecification"/>,
/// <see cref="RostersByEmployeesAndStatusSpecification"/>, <see cref="ShiftByIdSpecification"/>,
/// <see cref="HolidayByLocationAndDateSpecification"/>, <see cref="AttendanceDayByEmployeeAndDateSpecification"/>)
/// one employee at a time, each scoped by that employee's own carried <c>TenantId</c> rather than
/// an ambient one — and the same pure <see cref="AttendanceDayCalculator"/> — so the only genuinely
/// duplicated code is the cross-tenant employee enumeration and orchestration loop, not the
/// computation itself.
/// </para>
/// </summary>
public sealed class AttendanceDayComputationHostedService : BackgroundService
{
    private static readonly Action<ILogger, int, Exception?> LogSweepCompleted = LoggerMessage.Define<int>(
        LogLevel.Information, new EventId(1, nameof(LogSweepCompleted)), "Attendance-day computation sweep completed for {ComputedCount} employees");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BackgroundJobsOptions> _options;
    private readonly ILogger<AttendanceDayComputationHostedService> _logger;

    public AttendanceDayComputationHostedService(
        IServiceScopeFactory scopeFactory, IOptions<BackgroundJobsOptions> options, ILogger<AttendanceDayComputationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(_options.Value.AttendanceComputation.RunIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>Runs one sweep. Public so tests can drive it directly.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var employeesAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Employee>>();
        var locationsAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Location>>();
        var rostersAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<ShiftRoster>>();
        var shiftsAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Shift>>();
        var holidaysAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Domain.Attendance.Holiday>>();
        var attendanceDaysAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<AttendanceDay>>();
        var attendanceDayWriter = scope.ServiceProvider.GetRequiredService<IWriteRepository<AttendanceDay>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var timeZoneConverter = scope.ServiceProvider.GetRequiredService<ITimeZoneConverter>();

        var now = dateTimeProvider.UtcNow;

        // Every read below goes through IReadRepositoryAdmin<T> (ListIgnoringFiltersAsync), not
        // the ordinary tenant-scoped IReadRepository<T> — this hosted service has no ambient
        // ITenantContext (there's no authenticated request behind a background sweep), so the
        // normal tenant global-query-filter would silently return nothing for every entity type
        // here. Each entity still carries and is filtered by its own TenantId explicitly below.
        var activeEmployees = await employeesAdmin.ListIgnoringFiltersAsync(new ActiveEmployeesSpecification(), cancellationToken);

        var computedCount = 0;
        foreach (var employee in activeEmployees)
        {
            var matchingLocations = await locationsAdmin.ListIgnoringFiltersAsync(
                new LocationByIdSpecification(employee.TenantId, employee.LocationId), cancellationToken);
            if (matchingLocations.Count == 0)
            {
                continue;
            }

            var location = matchingLocations[0];
            var yesterday = timeZoneConverter.ResolveLocalDate(now, location.TimeZoneId).AddDays(-1);

            var matchingDays = await attendanceDaysAdmin.ListIgnoringFiltersAsync(
                new AttendanceDayByEmployeeAndDateSpecification(employee.TenantId, employee.Id, yesterday), cancellationToken);
            var isNewDay = matchingDays.Count == 0;
            var day = isNewDay ? AttendanceDay.Open(employee.TenantId, employee.Id, yesterday) : matchingDays[0];

            var rosterRows = await rostersAdmin.ListIgnoringFiltersAsync(
                new RostersByEmployeesAndStatusSpecification(employee.TenantId, [employee.Id.Value], ShiftRosterStatus.Published), cancellationToken);
            var assignedShiftId = RosterAssignmentResolver.Resolve(rosterRows, employee.Id, yesterday);
            Shift? assignedShift = null;
            if (assignedShiftId is { } shiftId)
            {
                var matchingShifts = await shiftsAdmin.ListIgnoringFiltersAsync(
                    new ShiftByIdSpecification(employee.TenantId, shiftId), cancellationToken);
                assignedShift = matchingShifts.Count > 0 ? matchingShifts[0] : null;
            }

            var matchingHolidays = await holidaysAdmin.ListIgnoringFiltersAsync(
                new HolidayByLocationAndDateSpecification(employee.TenantId, employee.LocationId, yesterday), cancellationToken);
            var isHoliday = matchingHolidays.Count > 0;

            // Same documented limitation as RecomputeAttendanceDayCommandHandler: the resolver
            // can't yet distinguish a real pattern-driven week-off from "unrostered."
            const bool isWeekOff = false;

            var punches = day.Punches.ToList();
            var firstInUtc = punches.Where(p => p.PunchType == PunchType.In)
                .OrderBy(p => p.PunchedAtUtc).Select(p => (DateTimeOffset?)p.PunchedAtUtc).FirstOrDefault();
            var lastOutUtc = punches.Where(p => p.PunchType == PunchType.Out)
                .OrderByDescending(p => p.PunchedAtUtc).Select(p => (DateTimeOffset?)p.PunchedAtUtc).FirstOrDefault();

            TimeOnly? firstInLocal = firstInUtc is { } inUtc
                ? ToTimeOnly(timeZoneConverter.ToZoned(inUtc, location.TimeZoneId))
                : null;
            TimeOnly? lastOutLocal = lastOutUtc is { } outUtc
                ? ToTimeOnly(timeZoneConverter.ToZoned(outUtc, location.TimeZoneId))
                : null;

            var result = AttendanceDayCalculator.Compute(punches, assignedShift, isHoliday, isWeekOff, firstInLocal, lastOutLocal);
            day.ApplyComputation(result, now, "system");

            if (isNewDay)
            {
                await attendanceDayWriter.AddAsync(day, cancellationToken);
            }
            else
            {
                attendanceDayWriter.Update(day);
            }

            computedCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogSweepCompleted(_logger, computedCount, null);
    }

    private static TimeOnly ToTimeOnly(NodaTime.ZonedDateTime zoned) => new(zoned.Hour, zoned.Minute, zoned.Second);

    private sealed class ActiveEmployeesSpecification : ISpecification<Employee>
    {
        public Expression<Func<Employee, bool>> Criteria => e => e.Status == EmploymentStatus.Active && !e.IsDeleted;

        public IReadOnlyList<Expression<Func<Employee, object>>> Includes { get; } = [];

        public IReadOnlyList<(Expression<Func<Employee, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

        public (int Skip, int Take)? Paging => null;
    }
}
