namespace Vespera.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "Vespera:BackgroundJobs";

    public OutboxDispatcherOptions Outbox { get; set; } = new();

    public RetentionPurgeOptions Retention { get; set; } = new();

    public OffboardingAccessRevocationOptions Offboarding { get; set; } = new();

    public AttendanceComputationOptions AttendanceComputation { get; set; } = new();

    public BiometricPollerOptions BiometricPoller { get; set; } = new();

    public SlaEscalationOptions SlaEscalation { get; set; } = new();

    public LeaveAccrualOptions LeaveAccrual { get; set; } = new();
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

public sealed class OffboardingAccessRevocationOptions
{
    public int RunIntervalHours { get; set; } = 24;
}

public sealed class AttendanceComputationOptions
{
    public int RunIntervalHours { get; set; } = 24;
}

/// <summary>A device poller runs far more often than the once-daily sweeps above — minutes, not
/// hours — since a punch sitting unsynced for a whole day defeats the point of the device.</summary>
public sealed class BiometricPollerOptions
{
    public int RunIntervalMinutes { get; set; } = 15;
}

public sealed class SlaEscalationOptions
{
    public int PollIntervalMinutes { get; set; } = 15;
}

/// <summary>Polls daily; posting is idempotent per calendar month (per
/// <see cref="Vespera.Domain.Leave.LeaveLedgerEntry.PeriodKey"/>), so an hourly-or-more interval
/// would just be wasted work, not a correctness requirement.</summary>
public sealed class LeaveAccrualOptions
{
    public int RunIntervalHours { get; set; } = 24;
}
