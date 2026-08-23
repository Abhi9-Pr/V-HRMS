using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Locations;
using Vespera.Domain.Attendance;
using Vespera.Infrastructure.Attendance;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>
/// Polls every active <see cref="BiometricDevice"/> for newly-reported punches and appends them
/// to the right <see cref="AttendanceDay"/>. Reads/writes cross-tenant via
/// <c>IReadRepositoryAdmin</c>/raw <see cref="VesperaDbContext"/> access, same shape as
/// <see cref="AttendanceDayComputationHostedService"/> — this hosted service has no ambient
/// <c>ITenantContext</c>, so it can't use <c>AttendanceDayResolver</c> (which takes the
/// tenant-scoped <c>IReadRepository&lt;T&gt;</c>); the find-or-open-yesterday's-day logic is
/// inlined here against each device's own carried <c>TenantId</c> instead, matching that hosted
/// service's precedent (<c>ResolveQuarantinedPunchCommandHandler</c>, which runs inside an
/// authenticated request, uses the real resolver — see its own doc comment).
/// </summary>
public sealed class BiometricPunchPollerHostedService : BackgroundService
{
    private static readonly Action<ILogger, int, int, Exception?> LogSweepCompleted = LoggerMessage.Define<int, int>(
        LogLevel.Information, new EventId(1, nameof(LogSweepCompleted)),
        "Biometric punch poll completed: {DeviceCount} device(s) polled, {IngestedCount} record(s) newly ingested");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BackgroundJobsOptions> _options;
    private readonly ILogger<BiometricPunchPollerHostedService> _logger;

    public BiometricPunchPollerHostedService(
        IServiceScopeFactory scopeFactory, IOptions<BackgroundJobsOptions> options, ILogger<BiometricPunchPollerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_options.Value.BiometricPoller.RunIntervalMinutes);

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

    /// <summary>Runs one poll of every active device. Public so tests can drive it directly.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var devicesAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<BiometricDevice>>();
        var deviceWriter = scope.ServiceProvider.GetRequiredService<IWriteRepository<BiometricDevice>>();
        var employeesAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Domain.Eis.Employee>>();
        var locationsAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Domain.Eis.Location>>();
        var attendanceDaysAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<AttendanceDay>>();
        var attendanceDayWriter = scope.ServiceProvider.GetRequiredService<IWriteRepository<AttendanceDay>>();
        var adapter = scope.ServiceProvider.GetRequiredService<IBiometricDeviceAdapter>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var timeZoneConverter = scope.ServiceProvider.GetRequiredService<ITimeZoneConverter>();

        var now = dateTimeProvider.UtcNow;
        var devices = await devicesAdmin.ListIgnoringFiltersAsync(new ActiveBiometricDevicesSpecification(), cancellationToken);

        var ingestedCount = 0;
        foreach (var device in devices)
        {
            var connection = new BiometricDeviceConnection(device.Host, device.Port, device.ApiKeyConfigurationKey);
            var fetchResult = await adapter.FetchSinceAsync(connection, device.Cursor, cancellationToken);

            foreach (var record in fetchResult.Records)
            {
                var alreadyIngested = await dbContext.Set<BiometricIngestionRecord>().AnyAsync(
                    r => r.BiometricDeviceId == device.Id.Value && r.ExternalRecordId == record.ExternalRecordId, cancellationToken);
                if (alreadyIngested)
                {
                    continue;
                }

                var matchingEmployees = await employeesAdmin.ListIgnoringFiltersAsync(
                    new EmployeeByBiometricDeviceUserIdSpecification(device.TenantId, record.DeviceUserId), cancellationToken);

                if (matchingEmployees.Count == 0)
                {
                    await dbContext.Set<QuarantinedBiometricPunch>().AddAsync(
                        QuarantinedBiometricPunch.Create(
                            device.TenantId, device.Id, record.DeviceUserId, record.PunchedAtUtc, record.PunchType, record.ExternalRecordId),
                        cancellationToken);
                }
                else
                {
                    var employee = matchingEmployees[0];
                    var matchingLocations = await locationsAdmin.ListIgnoringFiltersAsync(
                        new LocationByIdSpecification(employee.TenantId, employee.LocationId), cancellationToken);
                    if (matchingLocations.Count == 0)
                    {
                        continue;
                    }

                    var location = matchingLocations[0];
                    var today = timeZoneConverter.ResolveLocalDate(record.PunchedAtUtc, location.TimeZoneId);

                    var (day, isNewDay) = await ResolveAttendanceDayAsync(
                        attendanceDaysAdmin, employee.TenantId, employee.Id, today, cancellationToken);

                    var punchType = record.PunchType ?? (day.IsOpen ? PunchType.Out : PunchType.In);
                    var recordResult = day.RecordPunch(punchType, record.PunchedAtUtc, null, PunchSource.Biometric);
                    if (recordResult.IsSuccess)
                    {
                        if (isNewDay)
                        {
                            await attendanceDayWriter.AddAsync(day, cancellationToken);
                        }
                        else
                        {
                            attendanceDayWriter.Update(day);
                        }
                    }
                }

                await dbContext.Set<BiometricIngestionRecord>().AddAsync(
                    new BiometricIngestionRecord
                    {
                        Id = Guid.NewGuid(),
                        TenantId = device.TenantId.Value,
                        BiometricDeviceId = device.Id.Value,
                        ExternalRecordId = record.ExternalRecordId,
                        ProcessedAt = now,
                    },
                    cancellationToken);

                ingestedCount++;
            }

            if (fetchResult.NextCursor != device.Cursor)
            {
                device.UpdateCursor(fetchResult.NextCursor, now, "system");
                deviceWriter.Update(device);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogSweepCompleted(_logger, devices.Count, ingestedCount, null);
    }

    private static async Task<(AttendanceDay Day, bool IsNew)> ResolveAttendanceDayAsync(
        IReadRepositoryAdmin<AttendanceDay> attendanceDaysAdmin,
        Domain.Common.TenantId tenantId,
        Domain.Eis.EmployeeId employeeId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var todaysDays = await attendanceDaysAdmin.ListIgnoringFiltersAsync(
            new Vespera.Application.Features.Attendance.AttendanceDayByEmployeeAndDateSpecification(tenantId, employeeId, today), cancellationToken);
        if (todaysDays.Count > 0)
        {
            return (todaysDays[0], false);
        }

        var yesterday = today.AddDays(-1);
        var priorDays = await attendanceDaysAdmin.ListIgnoringFiltersAsync(
            new Vespera.Application.Features.Attendance.AttendanceDayByEmployeeAndDateSpecification(tenantId, employeeId, yesterday), cancellationToken);
        if (priorDays.Count > 0 && priorDays[0].IsOpen)
        {
            return (priorDays[0], false);
        }

        return (AttendanceDay.Open(tenantId, employeeId, today), true);
    }

    private sealed class ActiveBiometricDevicesSpecification : Application.Abstractions.Persistence.ISpecification<BiometricDevice>
    {
        public System.Linq.Expressions.Expression<Func<BiometricDevice, bool>> Criteria => d => d.IsActive && !d.IsDeleted;

        public IReadOnlyList<System.Linq.Expressions.Expression<Func<BiometricDevice, object>>> Includes { get; } = [];

        public IReadOnlyList<(System.Linq.Expressions.Expression<Func<BiometricDevice, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

        public (int Skip, int Take)? Paging => null;
    }
}
