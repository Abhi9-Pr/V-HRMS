using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Attendance.Regularizations;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/regularizations")]
public sealed class RegularizationsController : ControllerBase
{
    private readonly ISender _sender;

    public RegularizationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Submitted by the employee themselves, about their own attendance day — the handler
    /// checks the caller's linked employee matches the day being regularized.</summary>
    /// <response code="200">The new request's id.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Not this employee's own attendance day.</response>
    /// <response code="404">No such attendance day.</response>
    [HttpPost]
    [HasPermission(Permissions.Regularizations.Request)]
    [ProducesResponseType(typeof(SubmitRegularizationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit([FromForm] SubmitRegularizationRequest request, CancellationToken cancellationToken)
    {
        byte[]? content = null;
        if (request.Evidence is not null)
        {
            await using var stream = new MemoryStream();
            await request.Evidence.CopyToAsync(stream, cancellationToken);
            content = stream.ToArray();
        }

        var command = new SubmitRegularizationCommand(
            request.AttendanceDayId, request.Reason, request.Evidence?.FileName, content);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new SubmitRegularizationResponse(id)));
    }

    /// <summary>The caller's team inbox — every pending request they are currently the authorized
    /// approver for (their own reports, or someone else's via an active delegation).</summary>
    /// <response code="200">The actionable requests.</response>
    [HttpGet]
    [HasPermission(Permissions.Regularizations.ReadTeam)]
    [ProducesResponseType(typeof(IReadOnlyList<RegularizationRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetRegularizationsQuery(), cancellationToken)).ToActionResult(this);

    /// <response code="204">Approved.</response>
    /// <response code="403">Not the authorized approver for this request.</response>
    /// <response code="404">No such request.</response>
    /// <response code="409">Not pending.</response>
    [HttpPost("{requestId:guid}/approve")]
    [HasPermission(Permissions.Regularizations.Approve)]
    public async Task<IActionResult> Approve(Guid requestId, CancellationToken cancellationToken) =>
        (await _sender.Send(new ApproveRegularizationCommand(requestId), cancellationToken)).ToActionResult(this);

    /// <response code="204">Rejected.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Not the authorized approver for this request.</response>
    /// <response code="404">No such request.</response>
    /// <response code="409">Not pending.</response>
    [HttpPost("{requestId:guid}/reject")]
    [HasPermission(Permissions.Regularizations.Approve)]
    public async Task<IActionResult> Reject(
        Guid requestId, RejectRegularizationRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new RejectRegularizationCommand(requestId, request.RejectionReason), cancellationToken)).ToActionResult(this);
}

public sealed record SubmitRegularizationRequest(Guid AttendanceDayId, string Reason, IFormFile? Evidence);

public sealed record SubmitRegularizationResponse(Guid Id);

public sealed record RejectRegularizationRequest(string RejectionReason);
