using Vespera.Api.Extensions;
using Vespera.Api.Http;
using Vespera.Infrastructure.Attendance;
using Vespera.Infrastructure.BackgroundJobs;
using Vespera.Infrastructure.Notifications;
using Vespera.Infrastructure.Payroll;
using Vespera.Infrastructure.Security;
using Vespera.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
{
    ["--db-mode"] = "Vespera:Database:Mode",
});

builder.AddVesperaDatabase();
builder.AddVesperaIdentity();
builder.AddVesperaApplication();
builder.AddVesperaSwagger();

builder.Services.AddVesperaVirusScanning(builder.Configuration);
builder.Services.AddVesperaOcr(builder.Configuration);
builder.Services.AddVesperaNotificationChannels(builder.Configuration);
builder.Services.AddVesperaTimeZoneConversion();
builder.Services.AddVesperaBiometricDevices();
builder.Services.AddVesperaPayroll();
builder.Services.Configure<WebPunchOptions>(builder.Configuration.GetSection(WebPunchOptions.SectionName));
builder.Services.Configure<MobilePunchOptions>(builder.Configuration.GetSection(MobilePunchOptions.SectionName));

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
