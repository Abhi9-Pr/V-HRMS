using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;

namespace Vespera.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Defense in depth against a leaked cross-tenant reference: throws if any entity about to be
/// inserted carries a TenantId different from the ambient <see cref="ITenantContext"/>. Runs
/// before every other interceptor so a rejected insert is never stamped or audited as if it had
/// happened.
/// </summary>
public sealed class TenantGuardInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantGuardInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Guard(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Guard(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Guard(DbContext? context)
    {
        if (context is null || !_tenantContext.HasTenant)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added || entry.Entity is not ITenantScoped scoped)
            {
                continue;
            }

            if (scoped.TenantId != _tenantContext.TenantId)
            {
                throw new TenantIsolationViolationException(entry.Entity.GetType().Name, scoped.TenantId.Value, _tenantContext.TenantId.Value);
            }
        }
    }
}
