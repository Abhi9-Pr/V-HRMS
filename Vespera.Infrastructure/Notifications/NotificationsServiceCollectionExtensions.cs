using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Notifications;

public static class NotificationsServiceCollectionExtensions
{
    /// <summary>Registers the channels <see cref="NotificationDispatcher"/> fans out to that don't
    /// need an Api-layer dependency (the in-app <c>SignalRNotificationChannel</c> is registered
    /// separately in Api, since it needs <c>IHubContext</c>). Adding a channel is one more
    /// <c>AddSingleton&lt;INotificationChannel, ...&gt;()</c> line here — the dispatcher itself
    /// never changes.</summary>
    public static IServiceCollection AddVesperaNotificationChannels(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmtpOptions>().Bind(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<INotificationChannel, EmailNotificationChannel>();
        services.AddSingleton<INotificationChannel, StubPushNotificationChannel>();

        return services;
    }
}
