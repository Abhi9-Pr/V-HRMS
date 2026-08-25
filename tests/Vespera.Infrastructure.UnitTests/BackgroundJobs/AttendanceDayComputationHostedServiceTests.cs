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
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Repositories;
using Vespera.Infrastructure.Services;
using Vespera.Infrastructure.UnitTests.Persistence;

namespace Vespera.Infrastructure.UnitTests.BackgroundJobs;

public class AttendanceDayComputationHostedServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 2, 3, 0, 0, TimeSpan.Zero); // ~08:30 IST
    private static readonly GeoCoordinate SiteCoordinate = GeoCoordinate.Create(12.9716, 77.5946).Value;

    [Fact]
    public async Task RunOnceAsync_Should_Compute_Yesterdays_Day_And_Be_Idempotent_When_Run_Twice()
    {
        using var harness = new Harness();
        var tenantId = TenantId.New();

        var location = Location.Create(
            tenantId, "HQ", "1 Main St", "City", "Country", SiteCoordinate, "Asia/Kolkata", Now, "seed").Value;
        var employee = Employee.Onboard(
            tenantId, EmployeeCode.Create("EMP-500").Value, "Ada", "Lovelace",
            EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
            DepartmentId.New(), DesignationId.New(), location.Id, Now, "seed").Value;
        var shift = Shift.Create(tenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, Now, "seed").Value;

        // "Yesterday" in Asia/Kolkata relative to Now (03:00 UTC = 08:30 IST) is the local date one
        // day before Now's IST date.
        var yesterdayIst = new DateOnly(2026, 2, 1);
        var day = AttendanceDay.Open(tenantId, employee.Id, yesterdayIst);
        day.RecordPunch(PunchType.In, new DateTimeOffset(2026, 2, 1, 3, 30, 0, TimeSpan.Zero), null, PunchSource.Web); // 09:00 IST
        day.RecordPunch(PunchType.Out, new DateTimeOffset(2026, 2, 1, 12, 30, 0, TimeSpan.Zero), null, PunchSource.Web); // 18:00 IST

        harness.DbContext.Add(location);
        harness.DbContext.Add(employee);
        harness.DbContext.Add(shift);
        harness.DbContext.Add(day);
        await harness.DbContext.SaveChangesAsync(CancellationToken.None);

        await harness.Service.RunOnceAsync(CancellationToken.None);

        var persisted = await harness.DbContext.Set<AttendanceDay>().IgnoreQueryFilters().SingleAsync(d => d.Id == day.Id);
        persisted.WorkedMinutes.Should().Be(9 * 60);
        // No shift roster published in this fixture, but the employee did punch in/out -- that's
        // "Present" (no shift to compare against for a half-day threshold), not "Absent" (which
        // only applies to zero punches).
        persisted.Status.Should().Be(AttendanceDayStatus.Present);
        persisted.LastComputedAt.Should().NotBeNull();

        var firstComputedAt = persisted.LastComputedAt;
        var firstWorkedMinutes = persisted.WorkedMinutes;

        // Run again -- ApplyComputation is idempotent by construction, so a second sweep over the
        // same punches must reproduce the same worked-minutes, not drift or double-count.
        await harness.Service.RunOnceAsync(CancellationToken.None);

        var persistedAgain = await harness.DbContext.Set<AttendanceDay>().IgnoreQueryFilters().SingleAsync(d => d.Id == day.Id);
        persistedAgain.WorkedMinutes.Should().Be(firstWorkedMinutes);
        persistedAgain.LastComputedAt.Should().BeOnOrAfter(firstComputedAt!.Value);
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

            var services = new ServiceCollection();
            services.AddSingleton(DbContext);
            services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
            services.AddScoped(typeof(IReadRepositoryAdmin<>), typeof(ReadRepositoryAdmin<>));
            services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
            services.AddSingleton<IDateTimeProvider>(new FixedDateTimeProvider(Now));
            services.AddSingleton<ITimeZoneConverter, NodaTimeZoneConverter>();
            _provider = services.BuildServiceProvider();

            var options = Options.Create(new BackgroundJobsOptions());
            Service = new AttendanceDayComputationHostedService(
                _provider.GetRequiredService<IServiceScopeFactory>(), options, NullLogger<AttendanceDayComputationHostedService>.Instance);
        }

        public VesperaDbContext DbContext { get; }

        public AttendanceDayComputationHostedService Service { get; }

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
