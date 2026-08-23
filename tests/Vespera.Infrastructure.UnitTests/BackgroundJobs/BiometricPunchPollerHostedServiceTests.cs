using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Attendance;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Repositories;
using Vespera.Infrastructure.Services;
using Vespera.Infrastructure.UnitTests.Persistence;

namespace Vespera.Infrastructure.UnitTests.BackgroundJobs;

public class BiometricPunchPollerHostedServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 2, 3, 0, 0, TimeSpan.Zero);
    private static readonly GeoCoordinate SiteCoordinate = GeoCoordinate.Create(12.9716, 77.5946).Value;

    [Fact]
    public async Task RunOnceAsync_Should_Create_A_Real_Punch_For_A_Matched_DeviceUserId()
    {
        using var harness = new Harness();
        var (tenantId, device, employee) = await harness.SeedAsync(mapEmployeeToDevice: true);

        var record = new BiometricPunchRecord("ZK-001", new DateTimeOffset(2026, 2, 1, 3, 30, 0, TimeSpan.Zero), PunchType.In, "rec-1");
        harness.Adapter.FetchSinceAsync(Arg.Any<BiometricDeviceConnection>(), null, Arg.Any<CancellationToken>())
            .Returns(new BiometricFetchResult([record], "rec-1"));

        await harness.Service.RunOnceAsync(CancellationToken.None);

        var days = await harness.DbContext.Set<AttendanceDay>().IgnoreQueryFilters().Where(d => d.EmployeeId == employee.Id).ToListAsync();
        days.Should().ContainSingle();
        days[0].Punches.Should().ContainSingle(p => p.Source == PunchSource.Biometric && p.PunchType == PunchType.In);

        var refreshedDevice = await harness.DbContext.Set<BiometricDevice>().IgnoreQueryFilters().SingleAsync(d => d.Id == device.Id);
        refreshedDevice.Cursor.Should().Be("rec-1");
    }

    [Fact]
    public async Task RunOnceAsync_Should_Quarantine_An_Unmatched_DeviceUserId()
    {
        using var harness = new Harness();
        var (tenantId, device, _) = await harness.SeedAsync(mapEmployeeToDevice: false);

        var record = new BiometricPunchRecord("ZK-999", new DateTimeOffset(2026, 2, 1, 3, 30, 0, TimeSpan.Zero), PunchType.In, "rec-2");
        harness.Adapter.FetchSinceAsync(Arg.Any<BiometricDeviceConnection>(), null, Arg.Any<CancellationToken>())
            .Returns(new BiometricFetchResult([record], "rec-2"));

        await harness.Service.RunOnceAsync(CancellationToken.None);

        var quarantined = await harness.DbContext.Set<QuarantinedBiometricPunch>().IgnoreQueryFilters().ToListAsync();
        quarantined.Should().ContainSingle(q => q.DeviceUserId == "ZK-999" && q.Status == QuarantinedBiometricPunchStatus.Pending);

        var days = await harness.DbContext.Set<AttendanceDay>().IgnoreQueryFilters().ToListAsync();
        days.Should().BeEmpty();
    }

    [Fact]
    public async Task RunOnceAsync_Should_Not_Create_A_Second_Punch_For_A_Replayed_ExternalRecordId()
    {
        using var harness = new Harness();
        await harness.SeedAsync(mapEmployeeToDevice: true);

        var record = new BiometricPunchRecord("ZK-001", new DateTimeOffset(2026, 2, 1, 3, 30, 0, TimeSpan.Zero), PunchType.In, "rec-3");
        harness.Adapter.FetchSinceAsync(Arg.Any<BiometricDeviceConnection>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new BiometricFetchResult([record], "rec-3"));

        await harness.Service.RunOnceAsync(CancellationToken.None);
        await harness.Service.RunOnceAsync(CancellationToken.None);

        var days = await harness.DbContext.Set<AttendanceDay>().IgnoreQueryFilters().ToListAsync();
        days.Should().ContainSingle();
        days[0].Punches.Should().ContainSingle();
    }

    private sealed class Harness : IDisposable
    {
        private readonly SqliteVesperaDbContextFactory _factory = new();
        private readonly ServiceProvider _provider;

        public Harness()
        {
            var tenantContext = Substitute.For<ITenantContext>();
            tenantContext.HasTenant.Returns(false);

            DbContext = _factory.Create(tenantContext, new FakePiiProtector());

            Adapter = Substitute.For<IBiometricDeviceAdapter>();

            var services = new ServiceCollection();
            services.AddSingleton(DbContext);
            services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
            services.AddScoped(typeof(IReadRepositoryAdmin<>), typeof(ReadRepositoryAdmin<>));
            services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
            services.AddSingleton<IDateTimeProvider>(new FixedDateTimeProvider(Now));
            services.AddSingleton<ITimeZoneConverter, NodaTimeZoneConverter>();
            services.AddSingleton(Adapter);
            _provider = services.BuildServiceProvider();

            var options = Options.Create(new BackgroundJobsOptions());
            Service = new BiometricPunchPollerHostedService(
                _provider.GetRequiredService<IServiceScopeFactory>(), options, NullLogger<BiometricPunchPollerHostedService>.Instance);
        }

        public VesperaDbContext DbContext { get; }

        public IBiometricDeviceAdapter Adapter { get; }

        public BiometricPunchPollerHostedService Service { get; }

        public async Task<(TenantId TenantId, BiometricDevice Device, Employee Employee)> SeedAsync(bool mapEmployeeToDevice)
        {
            var tenantId = TenantId.New();
            var location = Location.Create(
                tenantId, "HQ", "1 Main St", "City", "Country", SiteCoordinate, "Asia/Kolkata", Now, "seed").Value;
            var employee = Employee.Onboard(
                tenantId, EmployeeCode.Create("EMP-600").Value, "Grace", "Hopper",
                EmailAddress.Create("grace@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
                new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
                DepartmentId.New(), DesignationId.New(), location.Id, Now, "seed").Value;
            if (mapEmployeeToDevice)
            {
                employee.AssignBiometricDeviceUserId("ZK-001", Now, "seed");
            }

            var device = BiometricDevice.Register(tenantId, location.Id, BiometricVendorType.ZKTeco, "device.local", 4370, null, Now, "seed").Value;

            DbContext.Add(location);
            DbContext.Add(employee);
            DbContext.Add(device);
            await DbContext.SaveChangesAsync(CancellationToken.None);

            return (tenantId, device, employee);
        }

        public void Dispose()
        {
            _provider.Dispose();
            DbContext.Dispose();
            _factory.Dispose();
        }
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }
}
