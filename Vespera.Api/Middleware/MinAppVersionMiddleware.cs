using Microsoft.Extensions.Options;
using Vespera.Api.Http;

namespace Vespera.Api.Middleware;

/// <summary>Force-upgrade gate for the Capacitor app: a request to <c>api/v{version}/mobile/*</c>
/// carrying an <c>X-App-Version</c> below the configured minimum for its <c>X-App-Platform</c>
/// gets a <c>426 Upgrade Required</c> instead of reaching a handler that may no longer be
/// compatible with it. A request with no version headers (a non-app caller — Swagger, a test, a
/// future non-mobile client hitting the same route prefix) is let through unchecked; there's
/// nothing to negotiate against.</summary>
public sealed class MinAppVersionMiddleware
{
    private const string AppVersionHeader = "X-App-Version";
    private const string AppPlatformHeader = "X-App-Platform";
    private const string MobileRouteSegment = "/mobile/";

    private readonly RequestDelegate _next;
    private readonly IOptions<MobileMinVersionOptions> _options;

    public MinAppVersionMiddleware(RequestDelegate next, IOptions<MobileMinVersionOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path is null || !path.Contains(MobileRouteSegment, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var appVersionRaw = context.Request.Headers[AppVersionHeader].FirstOrDefault();
        var platformRaw = context.Request.Headers[AppPlatformHeader].FirstOrDefault();

        if (!Version.TryParse(appVersionRaw, out var appVersion) || platformRaw is null)
        {
            await _next(context);
            return;
        }

        var minimumVersionRaw = platformRaw.ToLowerInvariant() switch
        {
            "ios" => _options.Value.Ios,
            "android" => _options.Value.Android,
            _ => null,
        };

        if (minimumVersionRaw is null || !Version.TryParse(minimumVersionRaw, out var minimumVersion) || appVersion >= minimumVersion)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
        await context.Response.WriteAsJsonAsync(new ForceUpgradeResponse(
            "force_upgrade_required", "This app version is no longer supported. Please update to continue.", minimumVersionRaw));
    }
}

public sealed record ForceUpgradeResponse(string Code, string Message, string MinimumVersion);
