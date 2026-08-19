using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;
using Vespera.Infrastructure.Provisioning.Probes;
using Vespera.Infrastructure.Provisioning.Provisioners;

namespace Vespera.Infrastructure.Provisioning;

public static class InfrastructureProvisioningServiceCollectionExtensions
{
    /// <summary>
    /// Pure DI wiring — no I/O happens here. The actual probing/provisioning runs the first
    /// time IConnectionStringResolver.ResolveAsync is called, which callers do explicitly
    /// (see Program.cs) after the host is built.
    /// </summary>
    public static IServiceCollection AddVesperaDatabaseProvisioning(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IValidateOptions<VesperaDatabaseOptions>, VesperaDatabaseOptionsValidator>();
        services.AddOptions<VesperaDatabaseOptions>()
            .Bind(configuration.GetSection(VesperaDatabaseOptions.SectionName))
            .ValidateOnStart();

        // Registration itself is config-driven (which candidates exist at all), so the bound
        // values are needed synchronously here, ahead of IOptions<T> being resolvable.
        var boundOptions = new VesperaDatabaseOptions();
        configuration.GetSection(VesperaDatabaseOptions.SectionName).Bind(boundOptions);

        services.AddSingleton<ExternalConnectionProbe>();
        services.AddSingleton<ExternalProvisioner>();
        services.AddSingleton<LocalPostgresProbe>();
        services.AddSingleton<LocalSqlServerProbe>();
        services.AddSingleton<LocalInstanceProvisioner>();
        services.AddSingleton<DockerDaemonProbe>();
        services.AddSingleton<DockerContainerProvisioner>();

        if (boundOptions.Fallback.UseSqlite)
        {
            services.AddSingleton<SqliteFallbackProbe>();
            services.AddSingleton<SqliteFallbackProvisioner>();
        }

        var localProbeType = boundOptions.Engine == DatabaseEngine.Postgres ? typeof(LocalPostgresProbe) : typeof(LocalSqlServerProbe);

        // Production must never resolve anything but External, regardless of Mode. Local/Docker/
        // SqliteFallback candidates are not even registered there, so Auto can't drift into them —
        // DatabaseProvisionerSelector also re-checks this at select time as defense in depth.
        var effectiveMode = environment.IsProduction() ? DatabaseMode.External : boundOptions.Mode;

        void AddCandidate(string name, DatabaseStrategyKind kind, Type probeType, Type provisionerType)
        {
            services.AddSingleton(sp => new DatabaseProvisioningCandidate(
                name,
                kind,
                (IDatabaseEnvironmentProbe)sp.GetRequiredService(probeType),
                (IDatabaseProvisioner)sp.GetRequiredService(provisionerType)));
        }

        switch (effectiveMode)
        {
            case DatabaseMode.External:
                AddCandidate("External", DatabaseStrategyKind.External, typeof(ExternalConnectionProbe), typeof(ExternalProvisioner));
                break;

            case DatabaseMode.Local:
                AddCandidate("Local", DatabaseStrategyKind.Local, localProbeType, typeof(LocalInstanceProvisioner));
                break;

            case DatabaseMode.Docker:
                AddCandidate("Docker", DatabaseStrategyKind.Docker, typeof(DockerDaemonProbe), typeof(DockerContainerProvisioner));
                break;

            case DatabaseMode.Auto:
            default:
                AddCandidate("External", DatabaseStrategyKind.External, typeof(ExternalConnectionProbe), typeof(ExternalProvisioner));
                AddCandidate("Local", DatabaseStrategyKind.Local, localProbeType, typeof(LocalInstanceProvisioner));
                AddCandidate("Docker", DatabaseStrategyKind.Docker, typeof(DockerDaemonProbe), typeof(DockerContainerProvisioner));
                if (boundOptions.Fallback.UseSqlite)
                {
                    AddCandidate(
                        "SqliteFallback", DatabaseStrategyKind.SqliteFallback, typeof(SqliteFallbackProbe), typeof(SqliteFallbackProvisioner));
                }

                break;
        }

        services.AddSingleton<IDatabaseProvisionerSelector, DatabaseProvisionerSelector>();
        services.AddSingleton<ProvisionedConnectionStringResolver>();
        services.AddSingleton<IConnectionStringResolver>(sp => sp.GetRequiredService<ProvisionedConnectionStringResolver>());

        return services;
    }
}
