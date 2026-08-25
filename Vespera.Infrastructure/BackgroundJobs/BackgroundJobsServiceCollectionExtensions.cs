using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Vespera.Infrastructure.BackgroundJobs;

public static class BackgroundJobsServiceCollectionExtensions
{
    /// <summary>
    /// The two hosted services only — <see cref="Application.Abstractions.Services.INotificationDispatcher"/>
    /// is registered unconditionally in <c>AddVesperaPersistence</c> instead, since command
    /// handlers like <c>ForgotPasswordCommandHandler</c> need it whether or not the background
    /// dispatcher/retention hosted services are running (e.g. under "IntegrationTesting", where
    /// this method is deliberately not called — see Program.cs).
    /// </summary>
    public static IServiceCollection AddVesperaBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BackgroundJobsOptions>()
            .Bind(configuration.GetSection(BackgroundJobsOptions.SectionName));

        services.AddSingleton<IRetentionTarget, AuditLogRetentionTarget>();

        services.AddHostedService<OutboxDispatcherHostedService>();
        services.AddHostedService<RetentionPurgeHostedService>();
        services.AddHostedService<OffboardingAccessRevocationHostedService>();
        services.AddHostedService<AttendanceDayComputationHostedService>();
        services.AddHostedService<BiometricPunchPollerHostedService>();
        services.AddHostedService<SlaEscalationHostedService>();

        return services;
    }
}
