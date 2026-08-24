using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/helpdesk/ticket-categories")]
public sealed class TicketCategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public TicketCategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new category's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Helpdesk.ManageConfiguration)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketCategoryRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new CreateTicketCategoryCommand(request.Name, request.DepartmentId, request.DefaultSlaPolicyId, idempotencyKey), cancellationToken))
            .ToActionResult(this, id => Ok(new { id }));

    [HttpGet]
    [HasPermission(Permissions.Helpdesk.ManageConfiguration)]
    public async Task<IActionResult> GetCategories([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetTicketCategoriesQuery(paging), cancellationToken)).ToActionResult(this);
}

public sealed record CreateTicketCategoryRequest(string Name, Guid DepartmentId, Guid? DefaultSlaPolicyId);
