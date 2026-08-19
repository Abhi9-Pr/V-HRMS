namespace Vespera.Domain.Common;

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());
}

public interface ITenantScoped
{
    public TenantId TenantId { get; }
}
