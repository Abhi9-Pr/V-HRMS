using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Designations;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/designations")]
public sealed class DesignationsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public DesignationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">A page of designations.</response>
    [HttpGet]
    [HasPermission(Permissions.Designations.Read)]
    [ProducesResponseType(typeof(PagedResult<DesignationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetDesignationsQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="200">The designation.</response>
    /// <response code="404">No such designation.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Designations.Read)]
    [ProducesResponseType(typeof(DesignationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetDesignationByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see CreateDesignationCommand's IIdempotentRequest.</summary>
    /// <response code="200">The new designation's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [HasPermission(Permissions.Designations.Manage)]
    [ProducesResponseType(typeof(CreateDesignationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateDesignationRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateDesignationCommand(request.Title, request.Grade, idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new CreateDesignationResponse(id)));
    }

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such designation.</response>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Designations.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateDesignationRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new UpdateDesignationCommand(id, request.Title, request.Grade), cancellationToken)).ToActionResult(this);

    /// <response code="204">Deleted.</response>
    /// <response code="404">No such designation.</response>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Designations.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteDesignationCommand(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateDesignationRequest(string Title, int Grade);

public sealed record UpdateDesignationRequest(string Title, int Grade);

public sealed record CreateDesignationResponse(Guid Id);
