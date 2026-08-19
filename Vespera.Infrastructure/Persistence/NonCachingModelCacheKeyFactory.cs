using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Vespera.Infrastructure.Persistence;

/// <summary>
/// EF Core caches the built model (and therefore the closures baked into any query filter) per
/// <see cref="DbContext"/> type by default, reusing whatever the very first constructed instance
/// produced. <see cref="VesperaDbContext.OnModelCreating"/> bakes the ambient
/// <c>ITenantContext.TenantId</c> value into the tenant global query filter at model-build time —
/// correct only if the model is rebuilt fresh for every (scoped, per-request) DbContext instance.
/// Returning a distinct key every time forces that rebuild, trading a small amount of per-request
/// model-building cost for tenant-isolation correctness.
/// </summary>
public sealed class NonCachingModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) => new object();
}
