using MediatR;
using Microsoft.Extensions.Logging;

namespace Vespera.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, Exception?> LogHandling = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(1, nameof(LogHandling)), "Handling {RequestName}");

    private static readonly Action<ILogger, string, Exception?> LogHandled = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(2, nameof(LogHandled)), "Handled {RequestName}");

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

        LogHandled(_logger, requestName, null);

        return response;
    }
}
