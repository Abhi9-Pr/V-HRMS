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
