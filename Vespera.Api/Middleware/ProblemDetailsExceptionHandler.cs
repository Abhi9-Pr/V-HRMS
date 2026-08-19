using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Vespera.Api.Middleware;

/// <summary>Catches anything a controller didn't turn into a Result-mapped response (see
/// ResultExtensions) and emits RFC 7807 ProblemDetails instead of an unhandled-exception page —
/// registered via <c>AddExceptionHandler&lt;T&gt;()</c> + <c>UseExceptionHandler()</c>.</summary>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private static readonly Action<ILogger, string, string, Exception> LogUnhandledException = LoggerMessage.Define<string, string>(
        LogLevel.Error, new EventId(1, nameof(LogUnhandledException)), "Unhandled exception for {Method} {Path}");

    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        LogUnhandledException(_logger, httpContext.Request.Method, httpContext.Request.Path, exception);

        var correlationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var value) ? value : null;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                Extensions = { ["correlationId"] = correlationId },
            },
            cancellationToken);

        return true;
    }
}
