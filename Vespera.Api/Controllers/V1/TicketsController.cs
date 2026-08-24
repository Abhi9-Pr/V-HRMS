using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Helpdesk;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/helpdesk/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new ticket's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Helpdesk.RaiseTickets)]
    public async Task<IActionResult> RaiseTicket(
        [FromBody] RaiseTicketRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new RaiseTicketCommand(request.CategoryId, request.Subject, request.Description, request.Priority, idempotencyKey), cancellationToken))
            .ToActionResult(this, id => Ok(new { id }));

    /// <response code="200">The uploaded attachment's storage reference.</response>
    [HttpPost("{ticketId:guid}/attachments")]
    [HasPermission(Permissions.Helpdesk.RaiseTickets)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAttachment(
        Guid ticketId, IFormFile file, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var command = new UploadTicketAttachmentCommand(ticketId, buffer.ToArray(), file.FileName, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, reference => Ok(new { reference }));
    }

    [HttpPost("{ticketId:guid}/comments")]
    [HasPermission(Permissions.Helpdesk.RaiseTickets)]
    public async Task<IActionResult> AddComment(
        Guid ticketId, [FromBody] AddCommentRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(
            new AddTicketCommentCommand(ticketId, request.Body, request.IsInternal, request.ParentCommentId, request.AttachmentReferences, idempotencyKey),
            cancellationToken))
            .ToActionResult(this);

    [HttpPost("{ticketId:guid}/assign")]
    [HasPermission(Permissions.Helpdesk.ManageTickets)]
    public async Task<IActionResult> Assign(
        Guid ticketId, [FromBody] AssignTicketRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new AssignTicketCommand(ticketId, request.EmployeeId, idempotencyKey), cancellationToken)).ToActionResult(this);

    [HttpPost("{ticketId:guid}/resolve")]
    [HasPermission(Permissions.Helpdesk.ManageTickets)]
    public async Task<IActionResult> Resolve(
        Guid ticketId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new ResolveTicketCommand(ticketId, idempotencyKey), cancellationToken)).ToActionResult(this);

    [HttpPost("{ticketId:guid}/close")]
    [HasPermission(Permissions.Helpdesk.ManageTickets)]
    public async Task<IActionResult> Close(
        Guid ticketId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new CloseTicketCommand(ticketId, idempotencyKey), cancellationToken)).ToActionResult(this);

    [HttpPost("{ticketId:guid}/satisfaction")]
    [HasPermission(Permissions.Helpdesk.RaiseTickets)]
    public async Task<IActionResult> RateSatisfaction(
        Guid ticketId, [FromBody] RateSatisfactionRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new RateTicketSatisfactionCommand(ticketId, request.Rating, idempotencyKey), cancellationToken)).ToActionResult(this);

    [HttpGet]
    [HasPermission(Permissions.Helpdesk.RaiseTickets)]
    public async Task<IActionResult> GetTickets([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetTicketsQuery(paging), cancellationToken)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Helpdesk.RaiseTickets)]
    public async Task<IActionResult> GetTicketById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetTicketByIdQuery(id), cancellationToken)).ToActionResult(this);

    [HttpGet("sla-compliance-report")]
    [HasPermission(Permissions.Helpdesk.ViewReports)]
    public async Task<IActionResult> GetSlaComplianceReport(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetSlaComplianceReportQuery(), cancellationToken)).ToActionResult(this);
}

public sealed record RaiseTicketRequest(Guid CategoryId, string Subject, string Description, TicketPriority Priority);

public sealed record AddCommentRequest(string Body, bool IsInternal, Guid? ParentCommentId, IReadOnlyList<string>? AttachmentReferences);

public sealed record AssignTicketRequest(Guid EmployeeId);

public sealed record RateSatisfactionRequest(int Rating);
