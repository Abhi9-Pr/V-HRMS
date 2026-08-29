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
[Route("api/v{version:apiVersion}/leave-types")]
public sealed class LeaveTypesController : ControllerBase
{
    private readonly ISender _sender;

    public LeaveTypesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Supports conditional GET — see docs/api-mobile-contract.md's reference-data
    /// caching convention.</summary>
    /// <response code="200">Every leave type configured for the tenant.</response>
    /// <response code="304">Nothing has changed since the given <c>If-None-Match</c> tag.</response>
    [HttpGet]
    [HasPermission(Permissions.Leave.Request)]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> ListLeaveTypes(CancellationToken cancellationToken)
    {
        var etagResult = await _sender.Send(new GetLeaveTypesETagQuery(), cancellationToken);
        if (etagResult.IsFailure)
        {
            return etagResult.ToActionResult(this);
        }

        if (ETagNegotiation.TryShortCircuit(HttpContext, etagResult.Value))
        {
            return new EmptyResult();
        }

        return (await _sender.Send(new GetLeaveTypesQuery(), cancellationToken)).ToActionResult(this);
    }

    /// <response code="200">The new leave type's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Leave.ManagePolicy)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateLeaveType(CreateLeaveTypeCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <response code="204">Eligibility rules updated.</response>
    /// <response code="404">No such leave type.</response>
    [HttpPut("{leaveTypeId:guid}/eligibility")]
    [HasPermission(Permissions.Leave.ManagePolicy)]
    public async Task<IActionResult> UpdateEligibility(
        Guid leaveTypeId, UpdateLeaveTypeEligibilityBody body, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new UpdateLeaveTypeEligibilityCommand(
                leaveTypeId, body.ApplicableGender, body.MinimumTenureMonths, body.IsEncashable, body.MaxEncashableDays),
            cancellationToken)).ToActionResult(this);
}

public sealed record UpdateLeaveTypeEligibilityBody(string? ApplicableGender, int MinimumTenureMonths, bool IsEncashable, decimal MaxEncashableDays);
