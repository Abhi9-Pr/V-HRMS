using Vespera.Domain.Common;

namespace Vespera.Application.Abstractions.Provisioning;

public interface IConnectionStringResolver
{
    public Task<string> ResolveAsync(TenantId tenantId, CancellationToken cancellationToken);
}
