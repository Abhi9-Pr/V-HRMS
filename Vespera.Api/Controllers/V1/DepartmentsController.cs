using Asp.Versioning;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Departments;

namespace Vespera.Api.Controllers.V1;

/// <summary>Reference CRUD slice — the pattern every later feature controller copies. See
/// docs/CONTRIBUTING-frontend.md.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;
    private readonly ICompactResponseContext _compactResponseContext;

    public DepartmentsController(ISender sender, ICompactResponseContext compactResponseContext)
    {
        _sender = sender;
        _compactResponseContext = compactResponseContext;
    }

    /// <summary>Supports conditional GET (see docs/api-mobile-contract.md) and the
    /// <c>X-Response-Shape: compact</c> negotiation — a mobile caller gets back
    /// <see cref="DepartmentSummaryDto"/> items instead of the full <see cref="DepartmentDto"/>.</summary>
    /// <response code="200">A page of departments.</response>
    /// <response code="304">Nothing has changed since the given <c>If-None-Match</c> tag.</response>
    [HttpGet]
    [HasPermission(Permissions.Departments.Read)]
    [ProducesResponseType(typeof(PagedResult<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken)
    {
        var etagResult = await _sender.Send(new GetDepartmentsETagQuery(), cancellationToken);
        if (etagResult.IsFailure)
        {
            return etagResult.ToActionResult(this);
        }

        if (ETagNegotiation.TryShortCircuit(HttpContext, etagResult.Value))
        {
            return new EmptyResult();
        }

        var result = await _sender.Send(new GetDepartmentsQuery(paging), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        if (_compactResponseContext.IsCompact)
        {
            return Ok(new PagedResult<DepartmentSummaryDto>(
                result.Value.Items.Adapt<List<DepartmentSummaryDto>>(), result.Value.Page, result.Value.PageSize, result.Value.TotalCount));
        }

        return Ok(result.Value);
    }

    /// <response code="200">The department.</response>
    /// <response code="404">No such department.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Departments.Read)]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetDepartmentByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see CreateDepartmentCommand's IIdempotentRequest.</summary>
    /// <response code="200">The new department's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [HasPermission(Permissions.Departments.Manage)]
    [ProducesResponseType(typeof(CreateDepartmentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateDepartmentCommand(request.Name, request.Code, request.ParentDepartmentId, idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new CreateDepartmentResponse(id)));
    }

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such department.</response>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Departments.Manage)]
    public async Task<IActionResult> Update(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new UpdateDepartmentCommand(id, request.Name, request.ParentDepartmentId), cancellationToken)).ToActionResult(this);

    /// <response code="204">Deleted.</response>
    /// <response code="404">No such department.</response>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Departments.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteDepartmentCommand(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateDepartmentRequest(string Name, string Code, Guid? ParentDepartmentId);

public sealed record UpdateDepartmentRequest(string Name, Guid? ParentDepartmentId);

public sealed record CreateDepartmentResponse(Guid Id);
