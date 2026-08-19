namespace Vespera.Application.Abstractions.Provisioning;

/// <summary>
/// OCP seam: a new provisioning strategy is added by registering another
/// IDatabaseProvisioner (paired with its probe) in the DI extension, never by editing this
/// selector's implementation.
/// </summary>
public interface IDatabaseProvisionerSelector
{
    public Task<IDatabaseProvisioner> SelectAsync(CancellationToken cancellationToken);
}
