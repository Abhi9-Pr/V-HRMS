using Vespera.Application.Abstractions.Provisioning;

namespace Vespera.Infrastructure.Provisioning;

/// <summary>
/// One registered (probe, provisioner) pair. The OCP seam: a new database source is added by
/// registering one more of these in InfrastructureProvisioningServiceCollectionExtensions —
/// DatabaseProvisionerSelector never needs to change.
/// </summary>
public sealed record DatabaseProvisioningCandidate(
    string Name, DatabaseStrategyKind Kind, IDatabaseEnvironmentProbe Probe, IDatabaseProvisioner Provisioner);
