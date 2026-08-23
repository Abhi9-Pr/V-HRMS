using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Holidays;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/holidays")]
public sealed class HolidaysController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public HolidaysController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Supports conditional GET — send back the previous response's <c>ETag</c> as
    /// <c>If-None-Match</c> to get a bare 304 when nothing's changed (see
    /// docs/api-mobile-contract.md's reference-data caching convention), scoped to the same
    /// optional <paramref name="locationId"/> filter as the list itself.</summary>
    /// <response code="200">A page of holidays, optionally filtered by location.</response>
    /// <response code="304">Nothing has changed since the given <c>If-None-Match</c> tag.</response>
    [HttpGet]
    [HasPermission(Permissions.Holidays.Read)]
    [ProducesResponseType(typeof(PagedResult<HolidayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> List(
        [FromQuery] PagedRequest paging, [FromQuery] Guid? locationId, CancellationToken cancellationToken)
    {
        var etagResult = await _sender.Send(new GetHolidaysETagQuery(locationId), cancellationToken);
        if (etagResult.IsFailure)
        {
            return etagResult.ToActionResult(this);
        }

        if (ETagNegotiation.TryShortCircuit(HttpContext, etagResult.Value))
        {
            return new EmptyResult();
        }

        return (await _sender.Send(new GetHolidaysQuery(paging, locationId), cancellationToken)).ToActionResult(this);
    }

    /// <response code="200">The holiday.</response>
    /// <response code="404">No such holiday.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Holidays.Read)]
    [ProducesResponseType(typeof(HolidayDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetHolidayByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see CreateHolidayCommand's IIdempotentRequest.</summary>
    /// <response code="200">The new holiday's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [HasPermission(Permissions.Holidays.Manage)]
    [ProducesResponseType(typeof(CreateHolidayResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateHolidayRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateHolidayCommand(request.LocationId, request.Date, request.Name, idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new CreateHolidayResponse(id)));
    }

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such holiday.</response>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Holidays.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateHolidayRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new UpdateHolidayCommand(id, request.Name, request.Date), cancellationToken)).ToActionResult(this);

    /// <response code="204">Deleted.</response>
    /// <response code="404">No such holiday.</response>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Holidays.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteHolidayCommand(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateHolidayRequest(Guid LocationId, DateOnly Date, string Name);

public sealed record UpdateHolidayRequest(string Name, DateOnly Date);

public sealed record CreateHolidayResponse(Guid Id);
