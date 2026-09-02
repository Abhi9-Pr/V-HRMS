using System.Diagnostics.Metrics;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Observability;
using Vespera.Domain.Common;
using Vespera.Infrastructure.Persistence;
using Vespera.Infrastructure.Persistence.Outbox;

namespace Vespera.Infrastructure.BackgroundJobs;

/// <summary>
/// Reads <see cref="OutboxMessageEntity"/> rows written by
/// <see cref="Persistence.Interceptors.DomainEventDispatchInterceptor"/> (one per raised domain
/// event) and <see cref="Persistence.Outbox.EfOutboxWriter"/> (explicit application-level writes),
/// and replays them: a domain-event row is wrapped as a <see cref="DomainEventNotification{TDomainEvent}"/>
/// and published through MediatR; anything else is deserialized as a <see cref="NotificationMessage"/>
/// and handed to <see cref="INotificationDispatcher"/>. Retries with capped exponential backoff and
/// dead-letters after <see cref="OutboxDispatcherOptions.MaxAttempts"/> failed attempts.
/// </summary>
public sealed class OutboxDispatcherHostedService : BackgroundService
{
    private static readonly Action<ILogger, Guid, int, Exception> LogDeadLettered = LoggerMessage.Define<Guid, int>(
        LogLevel.Error, new EventId(1, nameof(LogDeadLettered)), "Outbox message {MessageId} dead-lettered after {Attempts} attempts");

    private static readonly Action<ILogger, Guid, int, int, Exception> LogRetryScheduled = LoggerMessage.Define<Guid, int, int>(
        LogLevel.Warning, new EventId(2, nameof(LogRetryScheduled)), "Outbox message {MessageId} failed (attempt {Attempts}); retrying in {BackoffSeconds}s");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BackgroundJobsOptions> _options;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;

    // Age of the oldest still-pending row as of the last poll — an ObservableGauge is pulled by
    // the OTel SDK on its own export cadence, not pushed on every poll, so it just reads whatever
    // this field was last set to rather than triggering a DB query itself.
    private double _oldestPendingAgeSeconds;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory, IOptions<BackgroundJobsOptions> options, ILogger<OutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;

        VesperaMetrics.Meter.CreateObservableGauge(
            "vespera.outbox.lag_seconds", () => _oldestPendingAgeSeconds,
            unit: "s", description: "Age of the oldest pending outbox message as of the last dispatcher poll.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(_options.Value.Outbox.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOnceAsync(stoppingToken);

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

    /// <summary>Processes one batch of due outbox rows. Public so tests can drive the dispatcher
    /// directly without running it as a hosted background loop.</summary>
    public async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VesperaDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var notificationDispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var options = _options.Value.Outbox;

        var now = dateTimeProvider.UtcNow;

        // Ordering and the NextAttemptAt backoff check both happen client-side: EF's Sqlite
        // provider (used in unit tests, and the dev fallback at runtime) can neither order by
        // DateTimeOffset nor translate the combined nullable-OR condition alongside the enum
        // comparison. Pending rows are a small, actively-drained set in practice, so filtering
        // after a single fetch is a fine trade for portability across every supported provider.
        var pending = await dbContext.Set<OutboxMessageEntity>()
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .ToListAsync(cancellationToken);

        // The lag gauge reflects the whole pending backlog, not just this poll's due-and-batched
        // subset — a message stuck behind MaxBackoffSeconds retries should still show up as lag.
        _oldestPendingAgeSeconds = pending.Count > 0
            ? (now - pending.Min(m => m.OccurredOn)).TotalSeconds
            : 0;

        var due = pending
            .Where(m => m.NextAttemptAt is null || m.NextAttemptAt <= now)
            .OrderBy(m => m.OccurredOn)
            .Take(options.BatchSize)
            .ToList();

        foreach (var message in due)
        {
            using var correlationScope = message.CorrelationId is null
                ? null
                : _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = message.CorrelationId });

            try
            {
                await DispatchAsync(message, publisher, notificationDispatcher, cancellationToken);
                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedAt = dateTimeProvider.UtcNow;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message;

                if (message.Attempts >= options.MaxAttempts)
                {
                    message.Status = OutboxMessageStatus.DeadLettered;
                    LogDeadLettered(_logger, message.Id, message.Attempts, ex);
                }
                else
                {
                    var backoffSeconds = Math.Min(options.MaxBackoffSeconds, message.Attempts * message.Attempts);
                    message.NextAttemptAt = dateTimeProvider.UtcNow.AddSeconds(backoffSeconds);
                    LogRetryScheduled(_logger, message.Id, message.Attempts, backoffSeconds, ex);
                }
            }
        }

        if (due.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task DispatchAsync(
        OutboxMessageEntity message, IPublisher publisher, INotificationDispatcher notificationDispatcher, CancellationToken cancellationToken)
    {
        var payloadType = Type.GetType(message.Type)
            ?? throw new InvalidOperationException($"Outbox message {message.Id}: unresolvable payload type '{message.Type}'.");

        if (typeof(DomainEvent).IsAssignableFrom(payloadType))
        {
            var domainEvent = JsonSerializer.Deserialize(message.Payload, payloadType)
                ?? throw new InvalidOperationException($"Outbox message {message.Id}: payload deserialized to null.");

            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(payloadType);
            var notification = Activator.CreateInstance(notificationType, domainEvent)!;
            await publisher.Publish(notification, cancellationToken);
            return;
        }

        if (payloadType == typeof(NotificationMessage))
        {
            var notificationMessage = JsonSerializer.Deserialize<NotificationMessage>(message.Payload)
                ?? throw new InvalidOperationException($"Outbox message {message.Id}: payload deserialized to null.");
            await notificationDispatcher.DispatchAsync(notificationMessage, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Outbox message {message.Id}: unsupported payload type '{message.Type}'.");
    }
}
