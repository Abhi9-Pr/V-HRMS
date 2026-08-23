using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Eis;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees/{employeeId:guid}/reports-to")]
public sealed class ReportingRelationshipsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportingRelationshipsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The employee's full reporting history (active, future-dated, and closed).</response>
    [HttpGet]
    [HasPermission(Permissions.ReportingRelationships.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<ReportingRelationshipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid employeeId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetReportingRelationshipsQuery(employeeId), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new reporting relationship's id.</response>
    /// <response code="400">Validation failed, the range overlaps an existing line, or the line would create a cycle.</response>
    /// <response code="404">The employee or the manager does not exist.</response>
    [HttpPost]
    [HasPermission(Permissions.ReportingRelationships.Manage)]
    [ProducesResponseType(typeof(CreateReportingRelationshipResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(Guid employeeId, CreateReportingRelationshipRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new CreateReportingRelationshipCommand(employeeId, request.ManagerId, request.ValidFrom, request.ValidTo),
            cancellationToken)).ToActionResult(this, id => Ok(new CreateReportingRelationshipResponse(id)));

    /// <response code="204">Ended.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such reporting relationship.</response>
    [HttpPost("{id:guid}/end")]
    [HasPermission(Permissions.ReportingRelationships.Manage)]
    public async Task<IActionResult> End(Guid employeeId, Guid id, EndReportingRelationshipRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new EndReportingRelationshipCommand(employeeId, id, request.ValidTo), cancellationToken)).ToActionResult(this);
}

public sealed record CreateReportingRelationshipRequest(Guid ManagerId, DateOnly ValidFrom, DateOnly? ValidTo);

public sealed record EndReportingRelationshipRequest(DateOnly ValidTo);

public sealed record CreateReportingRelationshipResponse(Guid Id);
