using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Workspace;

namespace Vespera.Api.Controllers.V1;

/// <summary>Authoring/read-receipt actions for the announcements widget. Everyday reads
/// (<c>GetAnnouncementsForMeQuery</c>, acknowledge) are reachable here too so a non-dashboard
/// client (mobile, a future full announcements screen) doesn't have to go through
/// <c>/dashboard</c> for them; the widget itself gets its data from the aggregated call.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/announcements")]
public sealed class AnnouncementsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public AnnouncementsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">Every announcement currently live for the caller's audience.</response>
    [HttpGet("for-me")]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    [ProducesResponseType(typeof(IReadOnlyList<AnnouncementSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForMe(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetAnnouncementsForMeQuery(), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new announcement's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Workspace.ManageAnnouncements)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateAnnouncementCommand(
            request.Title, request.Body, request.AudienceScope, request.TargetDepartmentId, request.TargetLocationId,
            request.Priority, request.PublishAt, request.ExpiresAt, request.PublishImmediately, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }

    /// <response code="204">Published.</response>
    [HttpPost("{id:guid}/publish")]
    [HasPermission(Permissions.Workspace.ManageAnnouncements)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new PublishAnnouncementCommand(id), cancellationToken)).ToActionResult(this);

    /// <response code="204">Updated.</response>
    [HttpPut("{id:guid}/pinned")]
    [HasPermission(Permissions.Workspace.ManageAnnouncements)]
    public async Task<IActionResult> SetPinned(Guid id, [FromBody] bool pinned, CancellationToken cancellationToken) =>
        (await _sender.Send(new SetAnnouncementPinnedCommand(id, pinned), cancellationToken)).ToActionResult(this);

    /// <response code="204">Acknowledged.</response>
    [HttpPost("{id:guid}/acknowledge")]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new AcknowledgeAnnouncementCommand(id), cancellationToken)).ToActionResult(this);

    /// <summary>The HR compliance view: who in the announcement's audience has (and hasn't)
    /// acknowledged it.</summary>
    /// <response code="200">The read-receipts report.</response>
    [HttpGet("{id:guid}/receipts-report")]
    [HasPermission(Permissions.Workspace.ManageAnnouncements)]
    [ProducesResponseType(typeof(AnnouncementReceiptsReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiptsReport(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetAnnouncementReceiptsReportQuery(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateAnnouncementRequest(
    string Title,
    string Body,
    AnnouncementAudienceScope AudienceScope,
    Guid? TargetDepartmentId,
    Guid? TargetLocationId,
    AnnouncementPriority Priority,
    DateTimeOffset PublishAt,
    DateTimeOffset? ExpiresAt,
    bool PublishImmediately);
