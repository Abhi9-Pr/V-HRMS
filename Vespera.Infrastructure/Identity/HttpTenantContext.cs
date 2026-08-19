using Microsoft.AspNetCore.Http;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;

namespace Vespera.Infrastructure.Identity;

/// <summary>
/// Interim <see cref="ITenantContext"/>: reads an <c>X-Tenant-Id</c> header. There is no
/// authentication phase yet (see AGENTS.md — Identity is a separate, not-yet-built Infrastructure
/// responsibility), so this is deliberately the simplest thing that lets tenant isolation be
/// exercised now; a later phase swaps it for a JWT-claims-backed implementation without touching
/// any caller, since everything downstream only depends on <see cref="ITenantContext"/>.
/// Outside an HTTP request (background jobs), <see cref="HasTenant"/> is always false — by
/// design, since those flows either don't touch tenant-scoped data or use
/// <c>IReadRepositoryAdmin{T}</c> to cross tenants deliberately.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private const string TenantHeaderName = "X-Tenant-Id";

    private readonly TenantId _tenantId;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[TenantHeaderName].FirstOrDefault();
        HasTenant = header is not null && Guid.TryParse(header, out var parsed);
        _tenantId = HasTenant ? new TenantId(Guid.Parse(header!)) : default;
    }

    public bool HasTenant { get; }

    /// <summary>Default when <see cref="HasTenant"/> is false, never throws — code that builds
    /// query filters unconditionally (see VesperaDbContext) must stay safe to construct even for
    /// an unauthenticated/background DbContext instance.</summary>
    public TenantId TenantId => _tenantId;
}
