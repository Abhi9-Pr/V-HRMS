using Vespera.Api.Extensions;
using Vespera.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
{
    ["--db-mode"] = "Vespera:Database:Mode",
});

builder.AddVesperaDatabase();
builder.AddVesperaIdentity();
builder.AddVesperaApplication();
builder.AddVesperaSwagger();

// The hosted background services (outbox dispatcher, retention purge) poll the database from the
// moment the host starts, so they must not be registered under "Testing" (which stays entirely
// DB-free — see DatabaseServiceCollectionExtensions) or "IntegrationTesting" (which has a real
// database, but integration tests need to control outbox/retention timing themselves rather than
// race a live background dispatcher — see VesperaWebApplicationFactory).
if (!builder.Environment.IsEnvironment("Testing") && !builder.Environment.IsEnvironment("IntegrationTesting"))
{
    builder.Services.AddVesperaBackgroundJobs(builder.Configuration);
}

builder.AddVesperaObservability();

var app = builder.Build();

await app.InitializeVesperaDatabaseAsync();

app.UseVesperaMiddlewarePipeline();
app.MapVesperaEndpoints();

app.Run();

namespace Vespera.Api
{
    // Exposed so WebApplicationFactory<Program> can bootstrap integration tests.
    public partial class Program
    {
    }
}
