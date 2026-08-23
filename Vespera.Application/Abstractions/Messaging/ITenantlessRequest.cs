namespace Vespera.Application.Abstractions.Messaging;

/// <summary>
/// Opt-out marker for the rare request that is legitimately tenant-less — not "pre-auth" (login,
/// refresh, register, forgot-password all still require the pre-auth <c>X-Tenant-Id</c> header,
/// so they already satisfy TenantScopeBehavior), but requests whose entire purpose is to work
/// *without* knowing a tenant yet, e.g. resolving a tenant's id from its code. Marking a request
/// with this is a deliberate, reviewable exception — see GetTenantIdByCodeQuery for the only
/// current use.
/// </summary>
public interface ITenantlessRequest;
