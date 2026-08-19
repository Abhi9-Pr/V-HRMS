namespace Vespera.Application.Abstractions.Services;

public interface IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; }
}
