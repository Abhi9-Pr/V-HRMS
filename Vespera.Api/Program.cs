using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
{
    ["--db-mode"] = "Vespera:Database:Mode",
});

builder.Services.AddVesperaDatabaseProvisioning(builder.Configuration, builder.Environment);

var app = builder.Build();

// Skipped under the "Testing" environment (see Vespera.Api.IntegrationTests) so a plain health-
// check test doesn't need a real local Postgres/Docker daemon to start the app.
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.GetRequiredService<IConnectionStringResolver>().ResolveAsync(CancellationToken.None);
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
