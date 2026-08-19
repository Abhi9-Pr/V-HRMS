namespace Vespera.Application.Abstractions.Provisioning;

public enum DatabaseProvisioningStrategy
{
    SharedDatabase,
    DedicatedSchema,
    DedicatedDatabase,
}

/// <summary>
/// OCP seam: a new provisioning strategy is added by registering another
/// IDatabaseProvisioner, never by editing this selector's callers.
/// </summary>
public interface IDatabaseProvisionerSelector
{
    public IDatabaseProvisioner Select(DatabaseProvisioningStrategy strategy);
}
