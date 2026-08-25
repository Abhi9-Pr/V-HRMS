using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Eis;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
public sealed class EmployeesController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;
    private readonly IAuthorizationService _authorizationService;

    public EmployeesController(ISender sender, IAuthorizationService authorizationService)
    {
        _sender = sender;
        _authorizationService = authorizationService;
    }

    /// <response code="200">A page of employees.</response>
    [HttpGet]
    [HasPermission(Permissions.Employees.Read)]
    [ProducesResponseType(typeof(PagedResult<EmployeeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetEmployeesQuery(paging), cancellationToken)).ToActionResult(this);

    /// <summary>Employees.Read (broad roster access, same gate as List) OR self/subordinate/
    /// Employees.ReadAny (the narrower path for a caller with none of that, e.g. a plain Employee
    /// role viewing their own or a direct report's profile). See SubordinateOrSelfAuthorizationHandler.</summary>
    /// <response code="200">The employee.</response>
    /// <response code="403">Lacks Employees.Read, is not self, is not an active manager of this
    /// employee, and lacks Employees.ReadAny.</response>
    /// <response code="404">No such employee.</response>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] DateOnly? asOf, CancellationToken cancellationToken)
    {
        var hasBroadRead = await _authorizationService.AuthorizeAsync(User, PermissionAuthorizationPolicyProvider.PolicyName(Permissions.Employees.Read));
        if (!hasBroadRead.Succeeded)
        {
            var resourceAuthorization = await _authorizationService.AuthorizeAsync(
                User, new EmployeeId(id), new SubordinateOrSelfRequirement(Permissions.Employees.ReadAny));
            if (!resourceAuthorization.Succeeded)
            {
                return Forbid();
            }
        }

        return (await _sender.Send(new GetEmployeeByIdQuery(id, asOf), cancellationToken)).ToActionResult(this);
    }

    /// <summary>The only endpoint that ever returns a real PAN/bank-account/compensation value —
    /// every call is recorded via IPiiAccessAuditor.</summary>
    /// <response code="200">The revealed value.</response>
    /// <response code="403">Caller lacks EmployeeDocuments.Unmask.</response>
    /// <response code="404">No such employee, or that field has no value on record.</response>
    [HttpGet("{id:guid}/pii/{field}")]
    [HasPermission(Permissions.EmployeeDocuments.Unmask)]
    [ProducesResponseType(typeof(RevealPiiFieldResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevealPiiField(Guid id, EmployeePiiField field, CancellationToken cancellationToken) =>
        (await _sender.Send(new RevealEmployeePiiFieldQuery(id, field), cancellationToken))
            .ToActionResult(this, value => Ok(new RevealPiiFieldResponse(value)));

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see CreateEmployeeCommand's IIdempotentRequest.</summary>
    /// <response code="200">The new employee's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [HasPermission(Permissions.Employees.Write)]
    [ProducesResponseType(typeof(CreateEmployeeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new CreateEmployeeCommand(
            request.Code, request.FirstName, request.LastName, request.WorkEmail, request.Phone,
            request.DateOfBirth, request.DateOfJoining, request.DepartmentId, request.DesignationId, request.LocationId,
            idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new CreateEmployeeResponse(id)));
    }

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such employee.</response>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Employees.Write)]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new UpdateEmployeeCommand(
                id, request.FirstName, request.LastName, request.WorkEmail, request.Phone,
                request.Pan, request.BankAccount, request.AnnualCtcAmount, request.AnnualCtcCurrency),
            cancellationToken)).ToActionResult(this);

    /// <response code="204">Transferred.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such employee.</response>
    [HttpPost("{id:guid}/transfer")]
    [HasPermission(Permissions.Employees.Write)]
    public async Task<IActionResult> Transfer(Guid id, TransferEmployeeRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new TransferEmployeeCommand(id, request.DepartmentId, request.DesignationId, request.LocationId, request.EffectiveDate, request.Reason),
            cancellationToken)).ToActionResult(this);

    /// <response code="204">Exited.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such employee.</response>
    [HttpPost("{id:guid}/exit")]
    [HasPermission(Permissions.Employees.Write)]
    public async Task<IActionResult> Exit(Guid id, ExitEmployeeRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new ExitEmployeeCommand(id, request.ExitDate, request.Reason), cancellationToken)).ToActionResult(this);
}

public sealed record CreateEmployeeRequest(
    string Code, string FirstName, string LastName, string WorkEmail, string Phone,
    DateOnly DateOfBirth, DateOnly DateOfJoining, Guid DepartmentId, Guid DesignationId, Guid LocationId);

public sealed record UpdateEmployeeRequest(
    string FirstName, string LastName, string WorkEmail, string Phone,
    string? Pan, string? BankAccount, decimal? AnnualCtcAmount, string? AnnualCtcCurrency);

public sealed record TransferEmployeeRequest(Guid DepartmentId, Guid DesignationId, Guid LocationId, DateOnly EffectiveDate, string Reason);

public sealed record ExitEmployeeRequest(DateOnly ExitDate, string Reason);

public sealed record CreateEmployeeResponse(Guid Id);

public sealed record RevealPiiFieldResponse(string Value);
