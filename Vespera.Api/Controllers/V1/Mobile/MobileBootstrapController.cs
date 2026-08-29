using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Http;
using Vespera.Application.Features.Mobile;
using Vespera.Infrastructure.Identity;

namespace Vespera.Api.Controllers.V1.Mobile;

/// <summary>Everything the app needs on first launch, in one round trip — see
/// docs/api-mobile-contract.md. <see cref="GetMobileBootstrapQuery"/> composes the parts that come
/// from persistence (profile, shifts, geofences, reference-data version stamps); the two remaining
/// parts are read here instead, since they aren't persistence at all: granted permissions come
/// from the caller's own JWT claims (see <c>PermissionAuthorizationHandler</c> — permissions are
/// baked into the token at login, never re-resolved from the database per request), and enabled
/// feature flags is an empty placeholder — <c>IFeatureFlagService</c> exists as a port with no
/// implementation or seeded flags yet, and inventing either here would be scaffolding a feature
/// this phase doesn't own (see AGENTS.md's "do not scaffold future phases").</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/bootstrap")]
public sealed class MobileBootstrapController : ControllerBase
{
    private readonly ISender _sender;

    public MobileBootstrapController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">Everything the app needs to render its first screen.</response>
    /// <response code="404">The signed-in account isn't linked to an employee record.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(MobileBootstrapResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMobileBootstrapQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var permissions = User.FindAll(JwtClaimTypes.Permission).Select(claim => claim.Value).ToList();

        return Ok(new MobileBootstrapResponse(result.Value, permissions, EnabledFeatureFlags: []));
    }
}

public sealed record MobileBootstrapResponse(
    MobileBootstrapDto Bootstrap, IReadOnlyList<string> Permissions, IReadOnlyList<string> EnabledFeatureFlags);
