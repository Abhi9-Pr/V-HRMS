namespace Vespera.Application.Abstractions.Services;

public sealed record OutboxMessage(string Type, string Payload, DateTimeOffset OccurredOn);

public interface IOutboxWriter
{
    public Task WriteAsync(OutboxMessage message, CancellationToken cancellationToken);
}
