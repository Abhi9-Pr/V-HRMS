using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Identity;
using Vespera.Infrastructure.Notifications;
using Vespera.Infrastructure.Persistence.Idempotency;
using Vespera.Infrastructure.Persistence.Interceptors;
using Vespera.Infrastructure.Persistence.Outbox;
using Vespera.Infrastructure.Persistence.Repositories;
using Vespera.Infrastructure.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;
using Vespera.Infrastructure.Security;
using Vespera.Infrastructure.Services;

namespace Vespera.Infrastructure.Persistence;

public static class VesperaPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaPersistence(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddDataProtection();
        services.AddSingleton<IPiiProtector, DataProtectionPiiProtector>();

        // Registered unconditionally (not bundled with the background-jobs hosted services,
        // which are skipped under Testing/IntegrationTesting) — command handlers like
        // ForgotPasswordCommandHandler need a dispatcher whether or not those hosted services run.
        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();

        // Order matters: EF Core runs registered ISaveChangesInterceptor instances in
        // registration order. TenantGuard must reject a bad insert before anything else treats
        // it as having happened; AuditableEntity must stamp Created/Modified/RowVersion before
        // AuditLog snapshots CurrentValue; DomainEventDispatch adds the outbox rows last.
        services.AddScoped<ISaveChangesInterceptor, TenantGuardInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditLogInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DomainEventDispatchInterceptor>();

        services.AddDbContext<VesperaDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.ReplaceService<IModelCacheKeyFactory, NonCachingModelCacheKeyFactory>();

            var databaseOptions = sp.GetRequiredService<IOptions<VesperaDatabaseOptions>>().Value;
            var resolver = sp.GetRequiredService<ProvisionedConnectionStringResolver>();

            // Program.cs awaits IConnectionStringResolver.ResolveAsync before the host starts
            // serving requests (skipped only under the Testing environment, where nothing here
            // is exercised either), so by the time any VesperaDbContext is actually constructed
            // this call returns the already-cached connection string synchronously.
            var connectionString = resolver.ResolveAsync(CancellationToken.None).GetAwaiter().GetResult();

            if (!resolver.UsedSqliteFallback && databaseOptions.Engine == DatabaseEngine.Postgres)
            {
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(VesperaDbContext).Assembly.FullName));
            }
            else if (!resolver.UsedSqliteFallback && databaseOptions.Engine == DatabaseEngine.SqlServer)
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IVesperaDbContext>(sp => sp.GetRequiredService<VesperaDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<VesperaDbContext>());

        services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
        services.AddScoped(typeof(IReadRepositoryAdmin<>), typeof(ReadRepositoryAdmin<>));

        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IIdempotencyResponseCache, EfIdempotencyResponseCache>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();
        services.AddScoped<IPiiAccessAuditor, PiiAccessAuditor>();

        return services;
    }

    /// <summary>
    /// Prepares the schema for whichever provider was actually provisioned. Only the Postgres
    /// path ships real EF migrations (see Migrations/ — Postgres, Docker-provisioned or local, is
    /// the primary target: Docker's default image is Postgres and the Testcontainers integration
    /// tests exercise it). SQLite (the dev-only fallback) and SQL Server (a supported connect/
    /// provision target this phase, not yet a migrated one) use <c>EnsureCreated</c> instead —
    /// both are explicitly non-production paths already by the provisioning layer's own design.
    /// </summary>
    public static async Task ApplyVesperaPersistenceSchemaAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var resolver = scope.ServiceProvider.GetRequiredService<ProvisionedConnectionStringResolver>();
        var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<VesperaDatabaseOptions>>().Value;

        if (!resolver.UsedSqliteFallback && databaseOptions.Engine == DatabaseEngine.Postgres)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
