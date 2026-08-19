namespace Vespera.Domain.Common;

public interface ISoftDeletable
{
    public bool IsDeleted { get; }

    public DateTimeOffset? DeletedAt { get; }

    public string? DeletedBy { get; }
}
