using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Eis;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees/{employeeId:guid}/offboarding")]
public sealed class OffboardingController : ControllerBase
{
    private readonly ISender _sender;

    public OffboardingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Initiated automatically when the employee exits (see
    /// <c>EmployeeExitedDomainEventHandler</c>) — access revocation itself happens on a daily
    /// sweep once the exit date has passed (see <c>OffboardingAccessRevocationHostedService</c>);
    /// asset recovery and final settlement are confirmed here by a human.</summary>
    /// <response code="200">The checklist.</response>
    /// <response code="404">No checklist exists for this employee (they haven't exited yet).</response>
    [HttpGet]
    [HasPermission(Permissions.Offboarding.Read)]
    [ProducesResponseType(typeof(OffboardingChecklistDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetOffboardingChecklistByEmployeeIdQuery(employeeId), cancellationToken)).ToActionResult(this);

    /// <summary>A placeholder confirmation — flips the checklist item, does not itself call into
    /// the Assets module.</summary>
    /// <response code="204">Confirmed.</response>
    /// <response code="404">No such checklist.</response>
    /// <response code="409">Already confirmed.</response>
    [HttpPost("confirm-assets-recovered")]
    [HasPermission(Permissions.Offboarding.Manage)]
    public async Task<IActionResult> ConfirmAssetsRecovered(Guid employeeId, CancellationToken cancellationToken) =>
        (await _sender.Send(new ConfirmAssetsRecoveredCommand(employeeId), cancellationToken)).ToActionResult(this);

    /// <summary>A placeholder confirmation — flips the checklist item, does not itself call into
    /// the Payroll module.</summary>
    /// <response code="204">Confirmed.</response>
    /// <response code="404">No such checklist.</response>
    /// <response code="409">Already confirmed.</response>
    [HttpPost("confirm-final-settlement")]
    [HasPermission(Permissions.Offboarding.Manage)]
    public async Task<IActionResult> ConfirmFinalSettlement(Guid employeeId, CancellationToken cancellationToken) =>
        (await _sender.Send(new ConfirmFinalSettlementCommand(employeeId), cancellationToken)).ToActionResult(this);
}
