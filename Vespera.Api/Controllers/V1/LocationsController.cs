using Asp.Versioning;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Locations;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/locations")]
public sealed class LocationsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;
    private readonly ICompactResponseContext _compactResponseContext;

    public LocationsController(ISender sender, ICompactResponseContext compactResponseContext)
    {
        _sender = sender;
        _compactResponseContext = compactResponseContext;
    }

    /// <summary>Supports conditional GET (see docs/api-mobile-contract.md) and the
    /// <c>X-Response-Shape: compact</c> negotiation — a mobile caller gets back
    /// <see cref="LocationSummaryDto"/> items instead of the full <see cref="LocationDto"/>.</summary>
    /// <response code="200">A page of locations.</response>
    /// <response code="304">Nothing has changed since the given <c>If-None-Match</c> tag.</response>
    [HttpGet]
    [HasPermission(Permissions.Locations.Read)]
    [ProducesResponseType(typeof(PagedResult<LocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken)
    {
        var etagResult = await _sender.Send(new GetLocationsETagQuery(), cancellationToken);
        if (etagResult.IsFailure)
        {
            return etagResult.ToActionResult(this);
        }

        if (ETagNegotiation.TryShortCircuit(HttpContext, etagResult.Value))
        {
            return new EmptyResult();
        }

        var result = await _sender.Send(new GetLocationsQuery(paging), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        if (_compactResponseContext.IsCompact)
        {
            return Ok(new PagedResult<LocationSummaryDto>(
                result.Value.Items.Adapt<List<LocationSummaryDto>>(), result.Value.Page, result.Value.PageSize, result.Value.TotalCount));
        }

        return Ok(result.Value);
    }

    /// <response code="200">The location.</response>
    /// <response code="404">No such location.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Locations.Read)]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetLocationByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see CreateLocationCommand's IIdempotentRequest.</summary>
    /// <response code="200">The new location's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [HasPermission(Permissions.Locations.Manage)]
    [ProducesResponseType(typeof(CreateLocationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateLocationCommand(
            request.Name, request.AddressLine, request.City, request.Country,
            request.Latitude, request.Longitude, request.TimeZoneId, idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new CreateLocationResponse(id)));
    }

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such location.</response>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Locations.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new UpdateLocationCommand(id, request.Latitude, request.Longitude, request.AddressLine, request.City, request.Country),
            cancellationToken)).ToActionResult(this);

    /// <response code="204">Deleted.</response>
    /// <response code="404">No such location.</response>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Locations.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteLocationCommand(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateLocationRequest(
    string Name, string AddressLine, string City, string Country, double Latitude, double Longitude, string TimeZoneId);

public sealed record UpdateLocationRequest(double Latitude, double Longitude, string AddressLine, string City, string Country);

public sealed record CreateLocationResponse(Guid Id);
