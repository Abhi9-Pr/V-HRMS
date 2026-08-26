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
    /// Resolves the connection string, applies the schema, and (Development or IntegrationTesting
    /// only) seeds demo data. Skipped entirely under "Testing" — that environment stays DB-free so
    /// the trivial health check doesn't need a real database; see VesperaWebApplicationFactory for
    /// the distinct "IntegrationTesting" environment the rest of the integration tests use.
    /// </summary>
    public static async Task InitializeVesperaDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        await app.Services.GetRequiredService<IConnectionStringResolver>().ResolveAsync(CancellationToken.None);
        await app.Services.ApplyVesperaPersistenceSchemaAsync(CancellationToken.None);

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("IntegrationTesting"))
        {
            await DevelopmentSeeder.SeedAsync(app.Services, CancellationToken.None);
        }
    }
}
