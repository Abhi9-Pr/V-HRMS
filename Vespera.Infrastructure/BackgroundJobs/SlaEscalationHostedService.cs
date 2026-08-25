using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Helpdesk;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodic SLA sweep, mirroring <see cref="RetentionPurgeHostedService"/>'s exact shape: for
/// every still-open ticket across every tenant (a scheduled sweep is definitionally system-wide
/// work, the same justification the retention sweep uses for <see cref="IReadRepositoryAdmin{T}"/>),
/// runs the now-idempotent <see cref="Ticket.CheckSlaBreach"/>/<see cref="Ticket.CheckSlaWarning"/>
/// and lets <see cref="Persistence.Interceptors.DomainEventDispatchInterceptor"/> pick up whatever
/// events they raise into the outbox on the one <c>SaveChangesAsync</c> at the end of the batch.
/// </summary>
public sealed class SlaEscalationHostedService : BackgroundService
{
    private static readonly Action<ILogger, int, Exception?> LogSweepCompleted = LoggerMessage.Define<int>(
        LogLevel.Information, new EventId(1, nameof(LogSweepCompleted)), "SLA sweep completed against {TicketCount} open tickets");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BackgroundJobsOptions> _options;
    private readonly ILogger<SlaEscalationHostedService> _logger;

    public SlaEscalationHostedService(
        IServiceScopeFactory scopeFactory, IOptions<BackgroundJobsOptions> options, ILogger<SlaEscalationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_options.Value.SlaEscalation.PollIntervalMinutes);

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
        var tickets = scope.ServiceProvider.GetRequiredService<IReadRepositoryAdmin<Ticket>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var openTickets = await tickets.ListIgnoringFiltersAsync(new ActiveTicketsSpecification(), cancellationToken);

        foreach (var ticket in openTickets)
        {
            ticket.CheckSlaBreach(now);
            ticket.CheckSlaWarning(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogSweepCompleted(_logger, openTickets.Count, null);
    }

    private sealed class ActiveTicketsSpecification : ISpecification<Ticket>
    {
        public System.Linq.Expressions.Expression<Func<Ticket, bool>>? Criteria =>
            ticket => ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed;

        public IReadOnlyList<System.Linq.Expressions.Expression<Func<Ticket, object>>> Includes { get; } = [];

        public IReadOnlyList<(System.Linq.Expressions.Expression<Func<Ticket, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

        public (int Skip, int Take)? Paging => null;
    }
}
