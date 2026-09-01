using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;
using Vespera.Infrastructure.Provisioning;

namespace Vespera.Api.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    public static WebApplicationBuilder AddVesperaDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddVesperaDatabaseProvisioning(builder.Configuration, builder.Environment);
        builder.Services.AddVesperaPersistence(builder.Configuration);
        return builder;
    }

    /// <summary>
    /// Resolves the connection string and (outside Production) applies the schema automatically,
    /// then (Development or IntegrationTesting only) seeds demo data. Skipped entirely under
    /// "Testing" — that environment stays DB-free so the trivial health check doesn't need a real
    /// database; see VesperaWebApplicationFactory for the distinct "IntegrationTesting" environment
    /// the rest of the integration tests use.
    ///
    /// Production never migrates at startup: the schema is applied as a discrete deployment step
    /// via the EF migrations bundle (tools/publish-migrations-bundle.*) run against the target
    /// connection string before the new API image is rolled out. Migrating from inside the app
    /// process would mean every replica racing to apply the same migration on boot, and would tie
    /// a schema change to an API restart instead of letting it be sequenced independently (expand
    /// before contract, etc.) — see docs/docker-compose.md for how this differs from the
    /// docker-compose stack, which intentionally keeps today's auto-migrate behavior for local/CI
    /// use by running under ASPNETCORE_ENVIRONMENT=Development.
    /// </summary>
    public static async Task InitializeVesperaDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        await app.Services.GetRequiredService<IConnectionStringResolver>().ResolveAsync(CancellationToken.None);

        if (!app.Environment.IsProduction())
        {
            await app.Services.ApplyVesperaPersistenceSchemaAsync(CancellationToken.None);
        }

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("IntegrationTesting"))
        {
            await DevelopmentSeeder.SeedAsync(app.Services, CancellationToken.None);
        }
    }
}
