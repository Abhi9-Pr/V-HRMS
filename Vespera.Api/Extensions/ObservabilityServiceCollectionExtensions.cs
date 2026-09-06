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
    public const string PublicApiRateLimitPolicyName = "PublicApiRateLimit";
    public const string PublicJobsOutputCachePolicyName = "PublicJobs";

    public static WebApplicationBuilder AddVesperaObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddVesperaStorage(builder.Configuration);

        // Aggressive, short-lived caching for the anonymous public careers page — a cache miss
        // still goes through the same rate limiter as every other request (UseOutputCache is
        // registered after UseRateLimiter in the pipeline, see WebApplicationExtensions), so
        // caching never becomes a way to bypass the rate limit. Varies by X-Tenant-Id: the route
        // is identical for every tenant, so without this a cache hit could serve one tenant's job
        // postings to another — tenant isolation must hold for cached responses too, not just
        // uncached ones.
        builder.Services.AddOutputCache(options =>
            options.AddPolicy(
                PublicJobsOutputCachePolicyName,
                policy => policy.Expire(TimeSpan.FromMinutes(5)).SetVaryByHeader("X-Tenant-Id")));

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

        // Configurable (default 100/min/IP) rather than hardcoded: docker-compose.yml's dev/CI
        // stack (payroll-performance-budget, deploy-staging) seeds test data as dozens of
        // sequential single-record POSTs from one container IP — legitimate synthetic load that
        // has nothing to do with the abuse pattern this limiter defends against — and overrides
        // it higher there. Production keeps the 100 default; nothing here changes for it.
        var globalPermitLimit = builder.Configuration.GetValue("Vespera:RateLimiting:GlobalPermitLimit", 100);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = globalPermitLimit, Window = TimeSpan.FromMinutes(1) }));

            // Tighter on auth endpoints — see AuthController's [EnableRateLimiting].
            options.AddPolicy(AuthRateLimitPolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

            // Looser than auth (this is meant to be crawled/browsed by real anonymous traffic)
            // but still tighter than the global default — see PublicJobsController.
            options.AddPolicy(PublicApiRateLimitPolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
        });

        return builder;
    }

    private static string ClientKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
