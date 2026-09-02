using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.Extensions.Logging;
using Vespera.Application.Observability;
using Vespera.Domain.Common;

namespace Vespera.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private static readonly Action<ILogger, string, Exception?> LogHandling = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(1, nameof(LogHandling)), "Handling {RequestName}");

    private static readonly Action<ILogger, string, Exception?> LogHandled = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(2, nameof(LogHandled)), "Handled {RequestName}");

    private static readonly Action<ILogger, string, string, Exception?> LogFailed = LoggerMessage.Define<string, string>(
        LogLevel.Warning, new EventId(3, nameof(LogFailed)), "{RequestName} failed: {ErrorCode}");

    // Generic, not login-specific: any request's failed Result is counted here, tagged by request
    // name and error code — a "failed logins" dashboard is just this filtered to
    // request.name=LoginCommand, rather than bespoke instrumentation on that one handler.
    private static readonly Counter<long> FailedRequests = VesperaMetrics.Meter.CreateCounter<long>(
        "vespera.request.failed", description: "Requests whose Result was a failure, tagged by request name and error code.");

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        LogHandling(_logger, requestName, null);

        var response = await next();

        if (response.IsFailure)
        {
            LogFailed(_logger, requestName, response.Error.Code, null);
            FailedRequests.Add(
                1,
                new KeyValuePair<string, object?>("request.name", requestName),
                new KeyValuePair<string, object?>("error.code", response.Error.Code));
        }

        LogHandled(_logger, requestName, null);

        return response;
    }
}
