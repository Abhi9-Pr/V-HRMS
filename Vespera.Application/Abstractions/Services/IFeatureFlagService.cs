using Vespera.Domain.Common;

namespace Vespera.Application.Abstractions.Services;

public interface IFeatureFlagService
{
    public Task<bool> IsEnabledAsync(string flagKey, TenantId tenantId, CancellationToken cancellationToken);
}
