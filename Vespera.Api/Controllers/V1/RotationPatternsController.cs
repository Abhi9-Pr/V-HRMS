using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.RotationPatterns;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rotation-patterns")]
public sealed class RotationPatternsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public RotationPatternsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">A page of rotation patterns.</response>
    [HttpGet]
    [HasPermission(Permissions.RotationPatterns.Read)]
    [ProducesResponseType(typeof(PagedResult<RotationPatternDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetRotationPatternsQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="200">The rotation pattern.</response>
    /// <response code="404">No such rotation pattern.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.RotationPatterns.Read)]
    [ProducesResponseType(typeof(RotationPatternDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetRotationPatternByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see CreateRotationPatternCommand's IIdempotentRequest.</summary>
    /// <response code="200">The new rotation pattern's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [HasPermission(Permissions.RotationPatterns.Manage)]
    [ProducesResponseType(typeof(CreateRotationPatternResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateRotationPatternRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateRotationPatternCommand(request.Name, request.Days, idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new CreateRotationPatternResponse(id)));
    }

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such rotation pattern.</response>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.RotationPatterns.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateRotationPatternRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new UpdateRotationPatternCommand(id, request.Days), cancellationToken)).ToActionResult(this);

    /// <response code="204">Deleted.</response>
    /// <response code="404">No such rotation pattern.</response>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.RotationPatterns.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteRotationPatternCommand(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateRotationPatternRequest(string Name, IReadOnlyList<RotationPatternDayRequest> Days);

public sealed record UpdateRotationPatternRequest(IReadOnlyList<RotationPatternDayRequest> Days);

public sealed record CreateRotationPatternResponse(Guid Id);
