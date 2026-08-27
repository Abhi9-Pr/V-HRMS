using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Workspace;

namespace Vespera.Api.Controllers.V1;

/// <summary>Personal to-do list — every action here is self-service (the owner's own items only;
/// every handler resolves the owner from the signed-in account, never from the request), gated by
/// the same baseline permission as the dashboard itself.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/todos")]
public sealed class TodosController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public TodosController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The caller's to-do items, in their saved drag-drop order.</response>
    [HttpGet]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    [ProducesResponseType(typeof(IReadOnlyList<TodoItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyTodoItemsQuery(), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new to-do item's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateTodoItemRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateTodoItemCommand(request.Title, request.DueDate, request.Urgency, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }

    /// <response code="204">Updated.</response>
    [HttpPut("{id:guid}/done")]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    public async Task<IActionResult> SetDone(Guid id, [FromBody] bool done, CancellationToken cancellationToken) =>
        (await _sender.Send(new SetTodoItemDoneCommand(id, done), cancellationToken)).ToActionResult(this);

    /// <response code="204">Updated.</response>
    [HttpPut("{id:guid}/urgency")]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    public async Task<IActionResult> SetUrgency(Guid id, [FromBody] TodoUrgency urgency, CancellationToken cancellationToken) =>
        (await _sender.Send(new SetTodoItemUrgencyCommand(id, urgency), cancellationToken)).ToActionResult(this);

    /// <summary>The caller's to-do items, all of them, in the new drag-drop order.</summary>
    /// <response code="204">Reordered.</response>
    /// <response code="400">The list didn't contain exactly the caller's current items.</response>
    [HttpPost("reorder")]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    public async Task<IActionResult> Reorder([FromBody] IReadOnlyList<Guid> orderedTodoItemIds, CancellationToken cancellationToken) =>
        (await _sender.Send(new ReorderTodoItemsCommand(orderedTodoItemIds), cancellationToken)).ToActionResult(this);
}

public sealed record CreateTodoItemRequest(string Title, DateOnly? DueDate, TodoUrgency Urgency);
