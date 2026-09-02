using System.Diagnostics.Metrics;

namespace Vespera.Application.Observability;

/// <summary>Single shared <see cref="Meter"/> for every custom metric in the app — Program.cs's
/// OTel <c>MeterProvider</c> registers exactly this one name (<c>AddMeter(MeterName)</c>), so a new
/// metric anywhere in Application or Infrastructure just needs
/// <c>VesperaMetrics.Meter.CreateCounter/CreateHistogram/CreateObservableGauge</c> against this —
/// no new SDK registration required per metric.</summary>
public static class VesperaMetrics
{
    public const string MeterName = "Vespera";

    public static readonly Meter Meter = new(MeterName);
}
