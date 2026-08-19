using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Notifications;

namespace Vespera.Infrastructure.BackgroundJobs;

public static class BackgroundJobsServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BackgroundJobsOptions>()
            .Bind(configuration.GetSection(BackgroundJobsOptions.SectionName));

        services.AddSingleton<INotificationDispatcher, NotificationDispatcher>();

        services.AddSingleton<IRetentionTarget, AuditLogRetentionTarget>();

        services.AddHostedService<OutboxDispatcherHostedService>();
        services.AddHostedService<RetentionPurgeHostedService>();

        return services;
    }
}
