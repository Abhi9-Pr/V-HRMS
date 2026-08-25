using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Security;

public static class VirusScanServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaVirusScanning(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VirusScanOptions>()
            .Bind(configuration.GetSection(VirusScanOptions.SectionName));

        var provider = configuration.GetSection(VirusScanOptions.SectionName)["Provider"] ?? "Null";

        if (string.Equals(provider, "ClamAv", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IVirusScanner, ClamAvVirusScanner>();
        }
        else
        {
            services.AddSingleton<IVirusScanner, NullVirusScanner>();
        }

        return services;
    }
}
