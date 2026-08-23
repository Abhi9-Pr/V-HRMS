using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Shifts;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shifts")]
public sealed class ShiftsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public ShiftsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Supports conditional GET — send back the previous response's <c>ETag</c> as
    /// <c>If-None-Match</c> to get a bare 304 when no shift has changed (see
    /// docs/api-mobile-contract.md's reference-data caching convention). The tag is computed from
    /// just the tenant's shift ids/RowVersions, cheaply, before the full paged query runs.</summary>
    /// <response code="200">A page of shifts.</response>
    /// <response code="304">Nothing has changed since the given <c>If-None-Match</c> tag.</response>
    [HttpGet]
    [HasPermission(Permissions.Shifts.Read)]
    [ProducesResponseType(typeof(PagedResult<ShiftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken)
    {
        var etagResult = await _sender.Send(new GetShiftsETagQuery(), cancellationToken);
        if (etagResult.IsFailure)
        {
            return etagResult.ToActionResult(this);
        }

        if (ETagNegotiation.TryShortCircuit(HttpContext, etagResult.Value))
        {
            return new EmptyResult();
        }

        return (await _sender.Send(new GetShiftsQuery(paging), cancellationToken)).ToActionResult(this);
    }

    /// <response code="200">The shift.</response>
    /// <response code="404">No such shift.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Shifts.Read)]
    [ProducesResponseType(typeof(ShiftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetShiftByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see CreateShiftCommand's IIdempotentRequest.</summary>
    /// <response code="200">The new shift's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [HasPermission(Permissions.Shifts.Manage)]
    [ProducesResponseType(typeof(CreateShiftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateShiftRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateShiftCommand(
            request.Name, request.StartTime, request.EndTime, request.GraceMinutes, request.BreakMinutes, idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new CreateShiftResponse(id)));
    }

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such shift.</response>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Shifts.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateShiftRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new UpdateShiftCommand(id, request.Name, request.StartTime, request.EndTime, request.BreakMinutes), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Deleted.</response>
    /// <response code="404">No such shift.</response>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Shifts.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteShiftCommand(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateShiftRequest(string Name, TimeOnly StartTime, TimeOnly EndTime, int GraceMinutes, int BreakMinutes);

public sealed record UpdateShiftRequest(string Name, TimeOnly StartTime, TimeOnly EndTime, int BreakMinutes);

public sealed record CreateShiftResponse(Guid Id);
