namespace Vespera.Infrastructure.Persistence.Interceptors;

/// <summary>Thrown by <see cref="TenantGuardInterceptor"/> when code tries to insert a row whose
/// TenantId doesn't match the ambient tenant — always a programmer error (a leaked cross-tenant
/// reference), never an expected failure, so it's an exception rather than a Result.</summary>
public sealed class TenantIsolationViolationException : Exception
{
    public TenantIsolationViolationException(string entityName, Guid attemptedTenantId, Guid ambientTenantId)
        : base($"Attempted to insert a '{entityName}' for tenant '{attemptedTenantId}' while the ambient tenant is '{ambientTenantId}'.")
    {
    }
}
