using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Leave;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/blackout-periods")]
public sealed class BlackoutPeriodsController : ControllerBase
{
    private readonly ISender _sender;

    public BlackoutPeriodsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">Every blackout period configured for the tenant.</response>
    [HttpGet]
    [HasPermission(Permissions.Leave.Request)]
    [ProducesResponseType(typeof(IReadOnlyList<BlackoutPeriodDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBlackoutPeriods(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetBlackoutPeriodsQuery(), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new blackout period's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Leave.ManageBlackout)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateBlackoutPeriod(CreateBlackoutPeriodCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <response code="204">Removed.</response>
    /// <response code="404">No such blackout period.</response>
    [HttpDelete("{blackoutPeriodId:guid}")]
    [HasPermission(Permissions.Leave.ManageBlackout)]
    public async Task<IActionResult> DeleteBlackoutPeriod(Guid blackoutPeriodId, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteBlackoutPeriodCommand(blackoutPeriodId), cancellationToken)).ToActionResult(this);
}
