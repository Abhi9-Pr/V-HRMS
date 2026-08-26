using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Payroll;

namespace Vespera.Api.Controllers.V1.Finance;

/// <summary>Employee self-download (viewing one's own payslip) is not yet wired — every action here
/// requires Finance.Admin, same as the rest of this controller family.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/payslips")]
public sealed class PayslipsController : FinanceControllerBase
{
    private readonly ISender _sender;

    public PayslipsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The payslip's id (idempotent - re-generating an existing payslip returns the same id).</response>
    /// <response code="409">The payroll run has not been approved yet.</response>
    [HttpPost("generate")]
    [HasPermission(Permissions.Payroll.Write)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Generate([FromBody] GeneratePayslipCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <response code="200">A short-lived signed download URL for the payslip's password-protected PDF.</response>
    [HttpGet("{id:guid}/download-url")]
    [HasPermission(Permissions.Payroll.Read)]
    [ProducesResponseType(typeof(Uri), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetPayslipDownloadUrlQuery(id), cancellationToken)).ToActionResult(this);
}
