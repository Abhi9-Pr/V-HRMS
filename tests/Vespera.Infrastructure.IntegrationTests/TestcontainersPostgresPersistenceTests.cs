using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Testcontainers.PostgreSql;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Auditing;
using Vespera.Infrastructure.Persistence.Interceptors;
using Vespera.Infrastructure.Persistence.Outbox;
using Vespera.Infrastructure.Security;

namespace Vespera.Infrastructure.IntegrationTests;

/// <summary>
/// Exercises the real Phase 3 persistence stack — migrations, tenant isolation, redacted
/// auditing, transactional outbox — against a throwaway, Testcontainers-provisioned Postgres.
/// Skips (not fails) when no Docker daemon is reachable, mirroring
/// DockerContainerProvisioningIntegrationTests in Vespera.Infrastructure.UnitTests.
/// </summary>
public sealed class TestcontainersPostgresPersistenceTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        // No direct Docker.DotNet dependency here (it would collide with the Enhanced fork
        // Testcontainers itself depends on within this project) — availability is proven by
        // actually trying to start the container instead of pinging the daemon first.
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("vespera_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Migrations_Should_Apply_Tenant_Isolation_Should_Hold_Audit_Should_Redact_And_Outbox_Should_Deliver()
    {
        Skip.IfNot(_dockerAvailable, "Docker daemon is not reachable in this environment");

        var tenantA = TenantId.New();
        var tenantB = TenantId.New();
        var now = DateTimeOffset.UtcNow;

        // 1) Migrations apply against a Docker-provisioned Postgres.
        await using (var migrationContext = CreateContext(NullTenantContext.Instance, new PassthroughPiiProtector()))
        {
            await migrationContext.Database.MigrateAsync();
        }

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        currentUser.IpAddress.Returns("10.0.0.1");
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(now);

        var tenantAContext = new FixedTenantContext(tenantA);
        var piiProtector = new PassthroughPiiProtector();

        Guid departmentAId;
        await using (var writeContext = CreateContext(
            tenantAContext, piiProtector,
            new TenantGuardInterceptor(tenantAContext),
            new AuditableEntityInterceptor(dateTimeProvider, currentUser),
            new AuditLogInterceptor(currentUser),
            new DomainEventDispatchInterceptor()))
        {
            var department = Department.Create(tenantA, "Engineering", "ENG", null, now, "seed").Value;
            writeContext.Add(department);

            var employee = Employee.Onboard(
                tenantA, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
                EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
                new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 1), department.Id, DesignationId.New(), LocationId.New(),
                now, "seed").Value;
            writeContext.Add(employee);
            await writeContext.SaveChangesAsync();

            departmentAId = department.Id.Value;

            // 4) Update an entity -> redacted audit row.
            const string rawPan = "ABCDE1234F";
            employee.UpdateStatutoryDetails(PanNumber.Create(rawPan).Value, null, now, "seed");
            await writeContext.SaveChangesAsync();

            var auditRows = await writeContext.Set<AuditLogEntity>().ToListAsync();
            auditRows.Should().Contain(r => r.ActionType == AuditActionType.Modified && r.NewValueJson != null && !r.NewValueJson.Contains(rawPan));

            var outboxRows = await writeContext.Set<OutboxMessageEntity>().ToListAsync();
            outboxRows.Should().ContainSingle(r => r.Status == OutboxMessageStatus.Pending);
        }

        // Also seed a Department for a second tenant to prove isolation.
        var tenantBContext = new FixedTenantContext(tenantB);
        await using (var otherTenantWriteContext = CreateContext(tenantBContext, piiProtector, new TenantGuardInterceptor(tenantBContext)))
        {
            otherTenantWriteContext.Add(Department.Create(tenantB, "Sales", "SLS", null, now, "seed").Value);
            await otherTenantWriteContext.SaveChangesAsync();
        }

        // 2) Tenant isolation: tenant A's read context must not see tenant B's rows, and vice versa.
        await using (var tenantAReadContext = CreateContext(tenantAContext, piiProtector))
        {
            var departments = await tenantAReadContext.Set<Department>().ToListAsync();
            departments.Should().ContainSingle(d => d.Id.Value == departmentAId);
            departments.Should().OnlyContain(d => d.TenantId == tenantA);
        }

        await using (var tenantBReadContext = CreateContext(tenantBContext, piiProtector))
        {
            var departments = await tenantBReadContext.Set<Department>().ToListAsync();
            departments.Should().OnlyContain(d => d.TenantId == tenantB);
            departments.Should().NotContain(d => d.Id.Value == departmentAId);
        }

        // 3) Outbox delivers: a fresh dispatch pass publishes the pending row and marks it Processed.
        var publisher = Substitute.For<IPublisher>();
        await using (var dispatchContext = CreateContext(NullTenantContext.Instance, piiProtector))
        {
            var pending = await dispatchContext.Set<OutboxMessageEntity>().Where(m => m.Status == OutboxMessageStatus.Pending).ToListAsync();
            foreach (var message in pending)
            {
                var payloadType = Type.GetType(message.Type)!;
                var domainEvent = System.Text.Json.JsonSerializer.Deserialize(message.Payload, payloadType)!;
                var notificationType = typeof(Vespera.Application.Common.DomainEventNotification<>).MakeGenericType(payloadType);
                var notification = Activator.CreateInstance(notificationType, domainEvent)!;
                await publisher.Publish(notification);
                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedAt = now;
            }

            await dispatchContext.SaveChangesAsync();
        }

        await using (var verifyContext = CreateContext(NullTenantContext.Instance, piiProtector))
        {
            (await verifyContext.Set<OutboxMessageEntity>().ToListAsync()).Should().OnlyContain(m => m.Status == OutboxMessageStatus.Processed);
        }

        await publisher.Received(1).Publish(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    private VesperaDbContext CreateContext(ITenantContext tenantContext, IPiiProtector piiProtector, params ISaveChangesInterceptor[] interceptors)
    {
        // Must match AddVesperaPersistence's registration: without this, EF's default model
        // caching would reuse the tenant filter baked in by whichever context was built first,
        // breaking tenant isolation between the contexts this test constructs one after another.
        var optionsBuilder = new DbContextOptionsBuilder<VesperaDbContext>()
            .UseNpgsql(_container!.GetConnectionString())
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, NonCachingModelCacheKeyFactory>();

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new VesperaDbContext(optionsBuilder.Options, tenantContext, piiProtector);
    }

    private sealed class FixedTenantContext : ITenantContext
    {
        public FixedTenantContext(TenantId tenantId) => TenantId = tenantId;

        public TenantId TenantId { get; }

        public bool HasTenant => true;
    }

    private sealed class NullTenantContext : ITenantContext
    {
        public static readonly NullTenantContext Instance = new();

        public TenantId TenantId => default;

        public bool HasTenant => false;
    }

    private sealed class PassthroughPiiProtector : IPiiProtector
    {
        public string Protect(string plainText) => "enc:" + plainText;

        public string Unprotect(string protectedText) => protectedText.StartsWith("enc:", StringComparison.Ordinal) ? protectedText[4..] : protectedText;
    }
}
