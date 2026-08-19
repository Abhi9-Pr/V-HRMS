using Vespera.Domain.Common;

namespace Vespera.Application.Abstractions.Provisioning;

public interface IDatabaseProvisioner
{
    public Task ProvisionAsync(TenantId tenantId, CancellationToken cancellationToken);
}
