var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

namespace Vespera.Api
{
    // Exposed so WebApplicationFactory<Program> can bootstrap integration tests.
    public partial class Program
    {
    }
}
