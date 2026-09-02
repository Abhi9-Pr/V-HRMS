using Serilog;

namespace Vespera.Api.Extensions;

public static class LoggingServiceCollectionExtensions
{
    /// <summary>Replaces the default Microsoft.Extensions.Logging provider with Serilog, configured
    /// entirely from the "Serilog" appsettings section (sinks, levels — see appsettings.json /
    /// appsettings.Development.json) rather than in code, so an environment can change where logs
    /// go without a rebuild. <c>ReadFrom.Services</c> lets Serilog's own configuration-bound sinks
    /// (e.g. Seq) still receive DI-registered dependencies if they ever need them.
    ///
    /// This must run before <c>builder.Build()</c>, same as every other <c>Add*</c> extension —
    /// Serilog needs to own logging from the earliest possible point so nothing before it logs
    /// through the default provider instead.
    /// </summary>
    public static WebApplicationBuilder AddVesperaLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        return builder;
    }
}
