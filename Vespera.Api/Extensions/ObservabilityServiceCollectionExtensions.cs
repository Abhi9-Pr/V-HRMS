using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Vespera.Api.HealthChecks;
using Vespera.Api.Middleware;
using Vespera.Infrastructure.Storage;

namespace Vespera.Api.Extensions;

public static class ObservabilityServiceCollectionExtensions
{
    public const string CorsPolicyName = "VesperaCors";
    public const string AuthRateLimitPolicyName = "AuthRateLimit";

    public static WebApplicationBuilder AddVesperaObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddVesperaStorage();

        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<OutboxHealthCheck>("outbox", tags: ["ready"])
            .AddCheck<StorageHealthCheck>("storage", tags: ["ready"]);

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

        var allowedOrigins = builder.Configuration.GetSection("Vespera:Cors:AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options =>
        {
            // No wildcard, ever — an empty configured list means "no browser origin is allowed",
            // not "allow everything".
            options.AddPolicy(CorsPolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }
            });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));

            // Tighter on auth endpoints — see AuthController's [EnableRateLimiting].
            options.AddPolicy(AuthRateLimitPolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
        });

        return builder;
    }

    private static string ClientKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
