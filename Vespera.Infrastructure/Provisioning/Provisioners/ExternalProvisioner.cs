using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;

namespace Vespera.Infrastructure.Provisioning.Provisioners;

public sealed class ExternalProvisioner : IDatabaseProvisioner
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<VesperaDatabaseOptions> _options;

    public ExternalProvisioner(IConfiguration configuration, IOptions<VesperaDatabaseOptions> options)
    {
        _configuration = configuration;
        _options = options;
    }

    public Task<string> ProvisionAsync(CancellationToken cancellationToken)
    {
        var connectionStringName = _options.Value.External.ConnectionStringName
            ?? throw new InvalidOperationException("Vespera:Database:External:ConnectionStringName is not configured.");

        var connectionString = _configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{connectionStringName} is not configured.");

        return Task.FromResult(connectionString);
    }
}
