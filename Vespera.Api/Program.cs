using Vespera.Application;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Seed;
using Vespera.Infrastructure.Provisioning;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
{
    ["--db-mode"] = "Vespera:Database:Mode",
});

// MediatR + the CQRS pipeline behaviors — registered here (not previously wired into the API host)
// because the outbox dispatcher needs a resolvable IPublisher to replay domain events.
builder.Services.AddApplication();
builder.Services.AddVesperaDatabaseProvisioning(builder.Configuration, builder.Environment);
builder.Services.AddVesperaPersistence();

// The hosted services poll the database from the moment the host starts, so they must not be
// registered under "Testing" — same reason ResolveAsync/ApplyVesperaPersistenceSchemaAsync/
// DevelopmentSeeder are skipped there below (see Vespera.Api.IntegrationTests).
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddVesperaBackgroundJobs(builder.Configuration);
}

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.GetRequiredService<IConnectionStringResolver>().ResolveAsync(CancellationToken.None);
    await app.Services.ApplyVesperaPersistenceSchemaAsync(CancellationToken.None);

    if (app.Environment.IsDevelopment())
    {
        await DevelopmentSeeder.SeedAsync(app.Services, CancellationToken.None);
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

namespace Vespera.Api
{
    // Exposed so WebApplicationFactory<Program> can bootstrap integration tests.
    public partial class Program
    {
    }
}
