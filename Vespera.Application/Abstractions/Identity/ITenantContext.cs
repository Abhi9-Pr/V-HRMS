using Vespera.Domain.Common;

namespace Vespera.Application.Abstractions.Identity;

public interface ITenantContext
{
    public TenantId TenantId { get; }

    public bool HasTenant { get; }
}
