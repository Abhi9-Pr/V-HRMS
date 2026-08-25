using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Eis;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily sweep for the one offboarding step that has to happen automatically rather than on
/// human confirmation: revoking an exited employee's access once their last working day has
/// passed. Reads cross-tenant via <c>IReadRepositoryAdmin</c> — same shape as
/// <see cref="RetentionPurgeHostedService"/> — since this is definitionally system-wide work, not
/// a per-tenant request.
/// </summary>
public sealed class OffboardingAccessRevocationHostedService : BackgroundService
{
    private static readonly Action<ILogger, int, Exception?> LogSweepCompleted = LoggerMessage.Define<int>(
        LogLevel.Information, new EventId(1, nameof(LogSweepCompleted)), "Offboarding access-revocation sweep completed for {RevokedCount} employees");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BackgroundJobsOptions> _options;
    private readonly ILogger<OffboardingAccessRevocationHostedService> _logger;

    public OffboardingAccessRevocationHostedService(
        IServiceScopeFactory scopeFactory, IOptions<BackgroundJobsOptions> options, ILogger<OffboardingAccessRevocationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(_options.Value.Offboarding.RunIntervalHours);

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
        var checklistsAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<OffboardingChecklist>>();
        var checklistWriter = scope.ServiceProvider.GetRequiredService<IWriteRepository<OffboardingChecklist>>();
        var usersAdmin = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Domain.IdentityAccess.User>>();
        var accessRevocationService = scope.ServiceProvider.GetRequiredService<IAccessRevocationService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        // Filtered client-side for the same reason OutboxDispatcherHostedService/RetentionPurgeHostedService
        // do: the Sqlite provider used in tests (and the dev fallback at runtime) doesn't reliably
        // translate every DateOnly/enum comparison combination, and this is a small, actively-drained set.
        var pendingChecklists = (await checklistsAdmin.ListIgnoringFiltersAsync(new PendingAccessRevocationChecklistsSpecification(), cancellationToken))
            .Where(c => c.ExitDate <= today)
            .ToList();

        var revokedCount = 0;
        foreach (var checklist in pendingChecklists)
        {
            var users = await usersAdmin.ListIgnoringFiltersAsync(
                new UserByEmployeeIdSpecification(checklist.TenantId, checklist.EmployeeId), cancellationToken);
            var user = users.Count > 0 ? users[0] : null;
            if (user is not null)
            {
                await accessRevocationService.RevokeAccessAsync(user.Id.Value, cancellationToken);
            }

            var markResult = checklist.MarkAccessRevoked(now, "system");
            if (markResult.IsSuccess)
            {
                checklistWriter.Update(checklist);
                revokedCount++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogSweepCompleted(_logger, revokedCount, null);
    }

    private sealed class PendingAccessRevocationChecklistsSpecification : ISpecification<OffboardingChecklist>
    {
        public System.Linq.Expressions.Expression<Func<OffboardingChecklist, bool>>? Criteria =>
            c => c.AccessRevokedStatus == ChecklistItemStatus.Pending && !c.IsDeleted;

        public IReadOnlyList<System.Linq.Expressions.Expression<Func<OffboardingChecklist, object>>> Includes { get; } = [];

        public IReadOnlyList<(System.Linq.Expressions.Expression<Func<OffboardingChecklist, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

        public (int Skip, int Take)? Paging => null;
    }
}
