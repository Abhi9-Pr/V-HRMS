namespace Vespera.Application.Abstractions.Provisioning;

public interface IDatabaseProvisioner
{
    public Task<string> ProvisionAsync(CancellationToken cancellationToken);
}
