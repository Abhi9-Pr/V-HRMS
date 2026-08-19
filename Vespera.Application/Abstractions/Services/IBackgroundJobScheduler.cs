namespace Vespera.Application.Abstractions.Services;

/// <summary>
/// Deliberately payload-based rather than Expression&lt;Action&gt; so this port carries no
/// hint of a specific scheduler (e.g. Hangfire) into the Application layer.
/// </summary>
public interface IBackgroundJobScheduler
{
    public string Enqueue<TJob>(TJob jobPayload, CancellationToken cancellationToken) where TJob : notnull;

    public string Schedule<TJob>(TJob jobPayload, TimeSpan delay, CancellationToken cancellationToken) where TJob : notnull;

    public void Cancel(string jobId);
}
