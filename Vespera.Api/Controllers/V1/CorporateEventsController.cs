using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Workspace;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/corporate-events")]
public sealed class CorporateEventsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public CorporateEventsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">Every upcoming, non-cancelled corporate event, with the caller's own RSVP.</response>
    [HttpGet("upcoming")]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    [ProducesResponseType(typeof(IReadOnlyList<CorporateEventSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upcoming(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetUpcomingCorporateEventsQuery(), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new event's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Workspace.ManageEvents)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateCorporateEventRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateCorporateEventCommand(
            request.Title, request.Description, request.StartsAt, request.EndsAt, request.LocationText, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }

    /// <response code="204">Cancelled.</response>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Workspace.ManageEvents)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new CancelCorporateEventCommand(id), cancellationToken)).ToActionResult(this);

    /// <response code="204">Recorded.</response>
    [HttpPost("{id:guid}/rsvp")]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    public async Task<IActionResult> Rsvp(Guid id, [FromBody] RsvpResponse response, CancellationToken cancellationToken) =>
        (await _sender.Send(new RespondToEventRsvpCommand(id, response), cancellationToken)).ToActionResult(this);
}

public sealed record CreateCorporateEventRequest(string Title, string Description, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string LocationText);
