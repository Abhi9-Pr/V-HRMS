using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Leave;

namespace Vespera.Api.Controllers.V1;

/// <summary>"Holiday Mode" — a manager delegates their approvals for a date window.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proxy-delegations")]
public sealed class ProxyDelegationsController : ControllerBase
{
    private readonly ISender _sender;

    public ProxyDelegationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The caller's own delegations, active or revoked.</response>
    [HttpGet("mine")]
    [HasPermission(Permissions.Leave.ManageDelegation)]
    [ProducesResponseType(typeof(IReadOnlyList<ProxyDelegationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDelegations(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyDelegationsQuery(), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new delegation's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Leave.ManageDelegation)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateProxyDelegation(CreateProxyDelegationCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <response code="204">Revoked.</response>
    /// <response code="403">Not the delegator.</response>
    [HttpPost("{delegationId:guid}/revoke")]
    [HasPermission(Permissions.Leave.ManageDelegation)]
    public async Task<IActionResult> RevokeProxyDelegation(Guid delegationId, CancellationToken cancellationToken) =>
        (await _sender.Send(new RevokeProxyDelegationCommand(delegationId), cancellationToken)).ToActionResult(this);
}
