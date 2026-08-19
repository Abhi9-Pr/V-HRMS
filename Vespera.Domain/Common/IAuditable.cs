namespace Vespera.Domain.Common;

public interface IAuditable
{
    public DateTimeOffset CreatedAt { get; }

    public string CreatedBy { get; }

    public DateTimeOffset? ModifiedAt { get; }

    public string? ModifiedBy { get; }
}
