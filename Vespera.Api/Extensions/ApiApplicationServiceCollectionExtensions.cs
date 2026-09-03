using Asp.Versioning;
using Vespera.Api.Http;
using Vespera.Api.Notifications;
using Vespera.Api.Serialization;
using Vespera.Application;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Api.Extensions;

public static class ApiApplicationServiceCollectionExtensions
{
    public static WebApplicationBuilder AddVesperaApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplication();

        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new LenientDateOnlyJsonConverter()));

        builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        builder.Services.AddSignalR();

        // Mobile plumbing (see docs/api-mobile-contract.md) — the compact-response negotiation
        // context; the idempotency response cache is registered alongside the rest of Phase 3's
        // persistence ports in AddVesperaPersistence.
        builder.Services.AddScoped<ICompactResponseContext, CompactResponseContext>();

        // OCP: SignalR becomes just one more INotificationChannel registration — Phase 3's
        // NotificationDispatcher already fans out to every registered channel unchanged.
        builder.Services.AddSingleton<INotificationChannel, SignalRNotificationChannel>();

        // The dashboard's structured (non-toast) real-time push — see DashboardRealtimeBroadcaster.
        builder.Services.AddSingleton<IDashboardRealtimePublisher, DashboardRealtimeBroadcaster>();

        return builder;
    }
}
