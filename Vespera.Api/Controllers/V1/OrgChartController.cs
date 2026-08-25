using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.OrgChart;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/org-chart")]
public sealed class OrgChartController : ControllerBase
{
    private readonly ISender _sender;

    public OrgChartController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>The reporting hierarchy as of <paramref name="asOf"/>. Supports conditional GET —
    /// send back the previous response's <c>ETag</c> as <c>If-None-Match</c> to get a bare 304
    /// when nothing underneath has changed (see docs/api-mobile-contract.md's reference-data
    /// caching convention).</summary>
    /// <response code="200">The org chart tree (one or more roots).</response>
    /// <response code="304">Nothing has changed since the given <c>If-None-Match</c> tag.</response>
    /// <response code="404"><paramref name="rootEmployeeId"/> was given but no such employee exists.</response>
    [HttpGet]
    [HasPermission(Permissions.OrgChart.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<OrgChartNodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly asOf, [FromQuery] Guid? rootEmployeeId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrgChartQuery(asOf, rootEmployeeId), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        if (ETagNegotiation.TryShortCircuit(HttpContext, result.Value.ETag))
        {
            return new EmptyResult();
        }

        return Ok(result.Value.Roots);
    }
}
