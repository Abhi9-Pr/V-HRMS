using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Repositories;
using Vespera.Infrastructure.UnitTests.Persistence;

namespace Vespera.Infrastructure.UnitTests.BackgroundJobs;

public class OffboardingAccessRevocationHostedServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    [Fact]
    public async Task RunOnceAsync_Should_Revoke_Access_And_Mark_The_Checklist_Item_Done_For_A_Past_Exit_Date()
    {
        using var harness = new Harness();
        var tenantId = TenantId.New();
        var employee = CreateExitedEmployee(tenantId, Today.AddDays(-1));
        var user = User.Create(tenantId, EmailAddress.Create("exited@vespera.test").Value, employee.Id, Now, "hr@vespera.test");
        var checklist = OffboardingChecklist.Initiate(tenantId, employee.Id, employee.ExitDate!.Value, Now, "system").Value;

        harness.DbContext.Add(employee);
        harness.DbContext.Add(user);
        harness.DbContext.Add(checklist);
        await harness.DbContext.SaveChangesAsync(CancellationToken.None);

        await harness.Service.RunOnceAsync(CancellationToken.None);

        await harness.AccessRevocationService.Received(1).RevokeAccessAsync(user.Id.Value, Arg.Any<CancellationToken>());
        var persisted = await harness.DbContext.Set<OffboardingChecklist>().IgnoreQueryFilters().SingleAsync(c => c.Id == checklist.Id);
        persisted.AccessRevokedStatus.Should().Be(ChecklistItemStatus.Done);
        persisted.AccessRevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RunOnceAsync_Should_Not_Revoke_Access_For_A_Future_Exit_Date()
    {
        using var harness = new Harness();
        var tenantId = TenantId.New();
        var employee = CreateExitedEmployee(tenantId, Today.AddDays(5));
        var user = User.Create(tenantId, EmailAddress.Create("future-exit@vespera.test").Value, employee.Id, Now, "hr@vespera.test");
        var checklist = OffboardingChecklist.Initiate(tenantId, employee.Id, employee.ExitDate!.Value, Now, "system").Value;

        harness.DbContext.Add(employee);
        harness.DbContext.Add(user);
        harness.DbContext.Add(checklist);
        await harness.DbContext.SaveChangesAsync(CancellationToken.None);

        await harness.Service.RunOnceAsync(CancellationToken.None);

        await harness.AccessRevocationService.DidNotReceiveWithAnyArgs().RevokeAccessAsync(default, default);
        var persisted = await harness.DbContext.Set<OffboardingChecklist>().IgnoreQueryFilters().SingleAsync(c => c.Id == checklist.Id);
        persisted.AccessRevokedStatus.Should().Be(ChecklistItemStatus.Pending);
    }

    [Fact]
    public async Task RunOnceAsync_Should_Not_Reprocess_A_Checklist_Whose_Access_Is_Already_Revoked()
    {
        using var harness = new Harness();
        var tenantId = TenantId.New();
        var employee = CreateExitedEmployee(tenantId, Today.AddDays(-1));
        var user = User.Create(tenantId, EmailAddress.Create("already-revoked@vespera.test").Value, employee.Id, Now, "hr@vespera.test");
        var checklist = OffboardingChecklist.Initiate(tenantId, employee.Id, employee.ExitDate!.Value, Now, "system").Value;
        checklist.MarkAccessRevoked(Now, "system");

        harness.DbContext.Add(employee);
        harness.DbContext.Add(user);
        harness.DbContext.Add(checklist);
        await harness.DbContext.SaveChangesAsync(CancellationToken.None);

        await harness.Service.RunOnceAsync(CancellationToken.None);

        await harness.AccessRevocationService.DidNotReceiveWithAnyArgs().RevokeAccessAsync(default, default);
    }

    private static Employee CreateExitedEmployee(TenantId tenantId, DateOnly exitDate)
    {
        var employee = Employee.Onboard(
            tenantId, EmployeeCode.Create("EMP-900").Value, "Former", "Employee",
            EmailAddress.Create("former.employee@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
            DepartmentId.New(), DesignationId.New(), LocationId.New(), Now, "hr@vespera.test").Value;
        employee.Exit(exitDate, EmployeeExitReason.Resignation, Now, "hr@vespera.test");
        return employee;
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
            AccessRevocationService = Substitute.For<IAccessRevocationService>();

            var services = new ServiceCollection();
            services.AddSingleton(DbContext);
            services.AddScoped(typeof(IReadRepositoryAdmin<>), typeof(ReadRepositoryAdmin<>));
            services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
            services.AddSingleton(AccessRevocationService);
            services.AddSingleton<IDateTimeProvider>(new FixedDateTimeProvider(Now));
            _provider = services.BuildServiceProvider();

            var options = Options.Create(new BackgroundJobsOptions());
            Service = new OffboardingAccessRevocationHostedService(
                _provider.GetRequiredService<IServiceScopeFactory>(), options, NullLogger<OffboardingAccessRevocationHostedService>.Instance);
        }

        public VesperaDbContext DbContext { get; }

        public IAccessRevocationService AccessRevocationService { get; }

        public OffboardingAccessRevocationHostedService Service { get; }

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
