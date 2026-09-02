using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Vespera.Application.Observability;

namespace Vespera.Api.Extensions;

public static class TelemetryServiceCollectionExtensions
{
    /// <summary>Traces and metrics via the OpenTelemetry SDK — Serilog (<see
    /// cref="LoggingServiceCollectionExtensions"/>) owns structured logs separately; these two
    /// stay split rather than routing logs through OTel too, see docs/observability.md. When
    /// "Vespera:Otel:OtlpEndpoint" isn't configured (Testing/IntegrationTesting — see
    /// appsettings.Testing.json/appsettings.IntegrationTesting.json, neither sets it) no OTLP
    /// exporter is added at all, so tests never try to reach a collector that isn't there.
    ///
    /// HTTP/protobuf, not gRPC: the exporter here talks to the docker-compose otel-collector
    /// (never Seq directly — the collector re-exports to Seq's own OTLP endpoint, see
    /// otel-collector-config.yml), and gRPC would need TLS the collector isn't configured for.
    /// </summary>
    public static WebApplicationBuilder AddVesperaTelemetry(this WebApplicationBuilder builder)
    {
        var otlpEndpoint = builder.Configuration["Vespera:Otel:OtlpEndpoint"];

        var otelBuilder = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("Vespera.Api"));

        otelBuilder.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter(otlp => ConfigureOtlp(otlp, otlpEndpoint, "v1/traces"));
            }
        });

        otelBuilder.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(VesperaMetrics.MeterName);

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                metrics.AddOtlpExporter(otlp => ConfigureOtlp(otlp, otlpEndpoint, "v1/metrics"));
            }
        });

        return builder;
    }

    // The SDK does NOT append a per-signal path (/v1/traces, /v1/metrics) to a caller-supplied
    // Endpoint for HttpProtobuf — verified against a real otel-collector: without this, both
    // exporters POST to the bare base endpoint and get a 404 back. Only the SDK's own *default*
    // endpoint (unset Endpoint) gets that treatment.
    private static void ConfigureOtlp(OtlpExporterOptions options, string endpoint, string signalPath)
    {
        options.Endpoint = new Uri(new Uri(endpoint.TrimEnd('/') + "/"), signalPath);
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
}
