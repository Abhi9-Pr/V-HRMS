using Microsoft.AspNetCore.Authorization;

namespace Vespera.Api.Authorization;

/// <summary>The maker-checker primitive: succeeds only when the caller did not create the
/// resource being acted on. Generic over any <c>IAuditable</c> resource (see
/// NotCreatorAuthorizationHandler) — reusable beyond payroll finalize, the first consumer.</summary>
public sealed class NotCreatorRequirement : IAuthorizationRequirement
{
}
