using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Rosters;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rosters")]
public sealed class RostersController : ControllerBase
{
    private readonly ISender _sender;

    public RostersController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The resolved per-employee-per-day roster grid for the range.</response>
    [HttpGet]
    [HasPermission(Permissions.Rosters.Read)]
    [ProducesResponseType(typeof(RosterResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly rangeStart, [FromQuery] DateOnly rangeEnd, [FromQuery] Guid? departmentId,
        [FromQuery] Guid? employeeId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetRosterQuery(rangeStart, rangeEnd, departmentId, employeeId), cancellationToken)).ToActionResult(this);

    /// <response code="200">The number of draft roster entries created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such rotation pattern.</response>
    [HttpPost("generate")]
    [HasPermission(Permissions.Rosters.Manage)]
    [ProducesResponseType(typeof(GenerateRosterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Generate(GenerateRosterRequest request, CancellationToken cancellationToken)
    {
        var command = new GenerateRosterCommand(
            request.RotationPatternId, request.EmployeeIds, request.RangeStart, request.RangeEnd, request.PatternAnchorDate);

        return (await _sender.Send(command, cancellationToken))
            .ToActionResult(this, count => Ok(new GenerateRosterResponse(count)));
    }

    /// <response code="200">The number of roster entries published.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost("publish")]
    [HasPermission(Permissions.Rosters.Publish)]
    [ProducesResponseType(typeof(PublishRosterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(PublishRosterRequest request, CancellationToken cancellationToken)
    {
        var command = new PublishRosterCommand(request.EmployeeIds, request.RangeStart, request.RangeEnd);

        return (await _sender.Send(command, cancellationToken))
            .ToActionResult(this, count => Ok(new PublishRosterResponse(count)));
    }

    /// <response code="200">The new override entry's id.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such shift.</response>
    [HttpPost("overrides")]
    [HasPermission(Permissions.Rosters.Manage)]
    [ProducesResponseType(typeof(OverrideRosterAssignmentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Override(OverrideRosterAssignmentRequest request, CancellationToken cancellationToken)
    {
        var command = new OverrideRosterAssignmentCommand(request.EmployeeId, request.Date, request.ShiftId);

        return (await _sender.Send(command, cancellationToken))
            .ToActionResult(this, id => Ok(new OverrideRosterAssignmentResponse(id)));
    }
}

public sealed record GenerateRosterRequest(
    Guid RotationPatternId, IReadOnlyList<Guid> EmployeeIds, DateOnly RangeStart, DateOnly RangeEnd, DateOnly PatternAnchorDate);

public sealed record GenerateRosterResponse(int RosterEntriesCreated);

public sealed record PublishRosterRequest(IReadOnlyList<Guid> EmployeeIds, DateOnly RangeStart, DateOnly RangeEnd);

public sealed record PublishRosterResponse(int RosterEntriesPublished);

public sealed record OverrideRosterAssignmentRequest(Guid EmployeeId, DateOnly Date, Guid ShiftId);

public sealed record OverrideRosterAssignmentResponse(Guid Id);
