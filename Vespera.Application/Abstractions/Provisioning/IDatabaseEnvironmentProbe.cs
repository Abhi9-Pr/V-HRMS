using Vespera.Domain.Common;

namespace Vespera.Application.Abstractions.Provisioning;

public enum DatabaseEnvironmentStatus
{
    NotProvisioned,
    Provisioning,
    Ready,
    Faulted,
}

public interface IDatabaseEnvironmentProbe
{
    public Task<DatabaseEnvironmentStatus> ProbeAsync(TenantId tenantId, CancellationToken cancellationToken);
}
