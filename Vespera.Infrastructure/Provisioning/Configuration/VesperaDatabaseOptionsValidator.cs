using Microsoft.Extensions.Options;

namespace Vespera.Infrastructure.Provisioning.Configuration;

public sealed class VesperaDatabaseOptionsValidator : IValidateOptions<VesperaDatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, VesperaDatabaseOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Local.Host))
        {
            failures.Add("Vespera:Database:Local:Host must not be empty.");
        }

        if (options.Local.Port is <= 0 or > 65535)
        {
            failures.Add("Vespera:Database:Local:Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(options.Docker.Image))
        {
            failures.Add("Vespera:Database:Docker:Image must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Docker.ContainerName))
        {
            failures.Add("Vespera:Database:Docker:ContainerName must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.Docker.VolumeName))
        {
            failures.Add("Vespera:Database:Docker:VolumeName must not be empty.");
        }

        if (options.Docker.Port is <= 0 or > 65535)
        {
            failures.Add("Vespera:Database:Docker:Port must be between 1 and 65535.");
        }

        if (options.Docker.StartupTimeoutSeconds <= 0)
        {
            failures.Add("Vespera:Database:Docker:StartupTimeoutSeconds must be greater than zero.");
        }

        if (options.Mode == DatabaseMode.External && string.IsNullOrWhiteSpace(options.External.ConnectionStringName))
        {
            failures.Add("Vespera:Database:External:ConnectionStringName is required when Mode is External.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
