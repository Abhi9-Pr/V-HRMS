using Asp.Versioning;
using Mapster;
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
    private readonly ICompactResponseContext _compactResponseContext;

    public DesignationsController(ISender sender, ICompactResponseContext compactResponseContext)
    {
        _sender = sender;
        _compactResponseContext = compactResponseContext;
    }

    /// <summary>Supports conditional GET (see docs/api-mobile-contract.md) and the
    /// <c>X-Response-Shape: compact</c> negotiation — a mobile caller gets back
    /// <see cref="DesignationSummaryDto"/> items instead of the full <see cref="DesignationDto"/>.</summary>
    /// <response code="200">A page of designations.</response>
    /// <response code="304">Nothing has changed since the given <c>If-None-Match</c> tag.</response>
    [HttpGet]
    [HasPermission(Permissions.Designations.Read)]
    [ProducesResponseType(typeof(PagedResult<DesignationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken)
    {
        var etagResult = await _sender.Send(new GetDesignationsETagQuery(), cancellationToken);
        if (etagResult.IsFailure)
        {
            return etagResult.ToActionResult(this);
        }

        if (ETagNegotiation.TryShortCircuit(HttpContext, etagResult.Value))
        {
            return new EmptyResult();
        }

        var result = await _sender.Send(new GetDesignationsQuery(paging), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        if (_compactResponseContext.IsCompact)
        {
            return Ok(new PagedResult<DesignationSummaryDto>(
                result.Value.Items.Adapt<List<DesignationSummaryDto>>(), result.Value.Page, result.Value.PageSize, result.Value.TotalCount));
        }

        return Ok(result.Value);
    }

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
