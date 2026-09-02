using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.Extensions.Logging;
using Vespera.Application.Observability;

namespace Vespera.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMilliseconds = 500;

    private static readonly Action<ILogger, string, long, Exception?> LogSlowRequest = LoggerMessage.Define<string, long>(
        LogLevel.Warning, new EventId(1, nameof(LogSlowRequest)), "{RequestName} took {ElapsedMilliseconds}ms");

    // Recorded for EVERY request (not just the slow-path log above) — this is what backs the
    // request-latency dashboard, and payroll-run duration specifically is just this same histogram
    // filtered by request.name (e.g. FinalizePayrollRunCommand), rather than bespoke instrumentation
    // per payroll command.
    private static readonly Histogram<long> RequestDuration = VesperaMetrics.Meter.CreateHistogram<long>(
        "vespera.request.duration", unit: "ms", description: "MediatR request handler duration, including pipeline behaviors.");

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();
        RequestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("request.name", requestName));

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMilliseconds)
        {
            LogSlowRequest(_logger, requestName, stopwatch.ElapsedMilliseconds, null);
        }

        return response;
    }
}
