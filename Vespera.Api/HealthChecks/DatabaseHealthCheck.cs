using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Api.HealthChecks;

/// <summary>Provider-agnostic (CanConnectAsync works the same across Postgres/SQL Server/SQLite),
/// matching Phase 3's multi-provider persistence design.</summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly VesperaDbContext _dbContext;

    public DatabaseHealthCheck(VesperaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await _dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to the database.");
}
