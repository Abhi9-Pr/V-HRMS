namespace Vespera.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "Vespera:BackgroundJobs";

    public OutboxDispatcherOptions Outbox { get; set; } = new();

    public RetentionPurgeOptions Retention { get; set; } = new();
}

public sealed class OutboxDispatcherOptions
{
    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int MaxAttempts { get; set; } = 5;

    public int MaxBackoffSeconds { get; set; } = 300;
}

public sealed class RetentionPurgeOptions
{
    public int RunIntervalHours { get; set; } = 24;
}
