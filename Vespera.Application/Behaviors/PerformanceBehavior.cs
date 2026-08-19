using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Vespera.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMilliseconds = 500;

    private static readonly Action<ILogger, string, long, Exception?> LogSlowRequest = LoggerMessage.Define<string, long>(
        LogLevel.Warning, new EventId(1, nameof(LogSlowRequest)), "{RequestName} took {ElapsedMilliseconds}ms");

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMilliseconds)
        {
            LogSlowRequest(_logger, typeof(TRequest).Name, stopwatch.ElapsedMilliseconds, null);
        }

        return response;
    }
}
