using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Payroll;

namespace Vespera.Api.Controllers.V1.Finance;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/salary-structures")]
public sealed class SalaryStructuresController : FinanceControllerBase
{
    private readonly ISender _sender;

    public SalaryStructuresController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new structure's id.</response>
    /// <response code="400">A formula was invalid, or its component graph has a cycle.</response>
    /// <response code="409">Overlaps an existing structure for this employee.</response>
    [HttpPost]
    [HasPermission(Permissions.Payroll.Write)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreateSalaryStructureCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <response code="200">The structure active for this employee on the given date, or null if none exists.</response>
    [HttpGet("employees/{employeeId:guid}")]
    [HasPermission(Permissions.Payroll.Read)]
    [ProducesResponseType(typeof(SalaryStructureDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForEmployee(Guid employeeId, [FromQuery] DateOnly asOf, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetSalaryStructureQuery(employeeId, asOf), cancellationToken)).ToActionResult(this);
}
