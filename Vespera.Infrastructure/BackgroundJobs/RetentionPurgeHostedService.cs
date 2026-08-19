using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Compliance;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>
/// DPDP retention sweep: for every tenant's <see cref="RetentionPolicy"/> row that has a
/// registered <see cref="IRetentionTarget"/> for its category, purges/anonymizes data older than
/// the configured window. Reads cross-tenant via <c>IReadRepositoryAdmin</c> deliberately — a
/// retention sweep is definitionally system-wide work, one of the few legitimate callers of that
/// escape hatch.
/// </summary>
public sealed class RetentionPurgeHostedService : BackgroundService
{
    private static readonly Action<ILogger, int, Exception?> LogSweepCompleted = LoggerMessage.Define<int>(
        LogLevel.Information, new EventId(1, nameof(LogSweepCompleted)), "Retention sweep completed against {PolicyCount} policies");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BackgroundJobsOptions> _options;
    private readonly ILogger<RetentionPurgeHostedService> _logger;

    public RetentionPurgeHostedService(
        IServiceScopeFactory scopeFactory, IOptions<BackgroundJobsOptions> options, ILogger<RetentionPurgeHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(_options.Value.Retention.RunIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>Runs one sweep. Public so tests can drive it directly.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var policies = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<RetentionPolicy>>();
        var targets = scope.ServiceProvider.GetServices<IRetentionTarget>().ToDictionary(t => t.Category);
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var allPolicies = await policies.ListIgnoringFiltersAsync(new AllRetentionPoliciesSpecification(), cancellationToken);

        foreach (var policy in allPolicies)
        {
            if (!targets.TryGetValue(policy.EntityCategory, out var target))
            {
                continue;
            }

            var cutoff = now.AddDays(-policy.RetentionPeriodDays);
            await target.ApplyAsync(policy.TenantId.Value, cutoff, policy.Action, dbContext, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogSweepCompleted(_logger, allPolicies.Count, null);
    }

    private sealed class AllRetentionPoliciesSpecification : ISpecification<RetentionPolicy>
    {
        public System.Linq.Expressions.Expression<Func<RetentionPolicy, bool>>? Criteria => null;

        public IReadOnlyList<System.Linq.Expressions.Expression<Func<RetentionPolicy, object>>> Includes { get; } = [];

        public IReadOnlyList<(System.Linq.Expressions.Expression<Func<RetentionPolicy, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

        public (int Skip, int Take)? Paging => null;
    }
}
