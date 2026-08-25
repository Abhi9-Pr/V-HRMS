using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;

namespace Vespera.Infrastructure.Identity;

/// <summary>
/// For an authenticated request, the tenant comes from the JWT's own <c>tenant</c> claim (the
/// authoritative source once a token exists — a header would be spoofable at that point). Pre-auth
/// (login/register/refresh/forgot-password), there is no token yet, so these fall back to the
/// <c>X-Tenant-Id</c> header — that's how those commands know which tenant's data to look at
/// before any credential has been verified. Outside an HTTP request (background jobs),
/// <see cref="HasTenant"/> is always false — by design, since those flows either don't touch
/// tenant-scoped data or use <c>IReadRepositoryAdmin{T}</c> to cross tenants deliberately.
///
/// CAUTION: this reads <c>HttpContext.User</c> once, in the constructor, and never again — it is
/// only correct if nothing resolves <c>ITenantContext</c> (directly or transitively) before
/// <c>UseAuthentication()</c> has run for the request. A middleware registered earlier in the
/// pipeline that takes <c>ITenantContext</c> as an <c>InvokeAsync</c> *parameter* would trigger
/// that resolution too early — ASP.NET Core resolves all of a middleware's extra parameters
/// before its body runs, i.e. before that middleware even calls <c>next()</c> — silently freezing
/// this instance as unauthenticated for the rest of the request's DI scope. If a middleware
/// genuinely needs the tenant *after* the pipeline has run (e.g. for a post-request log line), it
/// must resolve it explicitly via <c>context.RequestServices.GetRequiredService&lt;ITenantContext&gt;()</c>
/// after <c>await next(context)</c>, not as an <c>InvokeAsync</c> parameter — see
/// RequestLoggingMiddleware for the fixed pattern.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private const string TenantHeaderName = "X-Tenant-Id";

    private readonly TenantId _tenantId;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        if (user?.Identity?.IsAuthenticated == true && Guid.TryParse(user.FindFirstValue(JwtClaimTypes.TenantId), out var claimTenantId))
        {
            HasTenant = true;
            _tenantId = new TenantId(claimTenantId);
            return;
        }

        var header = httpContext?.Request.Headers[TenantHeaderName].FirstOrDefault();
        HasTenant = header is not null && Guid.TryParse(header, out var headerTenantId);
        _tenantId = HasTenant ? new TenantId(Guid.Parse(header!)) : default;
    }

    public bool HasTenant { get; }

    /// <summary>Default when <see cref="HasTenant"/> is false, never throws — code that builds
    /// query filters unconditionally (see VesperaDbContext) must stay safe to construct even for
    /// an unauthenticated/background DbContext instance.</summary>
    public TenantId TenantId => _tenantId;
}
