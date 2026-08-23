using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vespera.Api.Extensions;
using Vespera.Api.Http;
using Vespera.Application.Features.Tenants;

namespace Vespera.Api.Controllers.V1;

/// <summary>Anonymous — resolves the tenant GUID a login screen needs for the pre-auth
/// <c>X-Tenant-Id</c> header from a human-readable tenant code. Rate-limited like the rest of
/// auth since it's anonymous and the code space is guessable.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants")]
[EnableRateLimiting(ObservabilityServiceCollectionExtensions.AuthRateLimitPolicyName)]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The tenant's id and name.</response>
    /// <response code="404">No tenant matches that code.</response>
    [HttpGet("by-code/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TenantLookupDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ByCode(string code, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetTenantIdByCodeQuery(code), cancellationToken)).ToActionResult(this);
}
