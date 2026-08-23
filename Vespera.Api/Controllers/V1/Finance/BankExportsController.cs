using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Payroll;

namespace Vespera.Api.Controllers.V1.Finance;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/bank-exports")]
public sealed class BankExportsController : FinanceControllerBase
{
    private readonly ISender _sender;

    public BankExportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The formatted transfer file plus a reconciliation report for the bank's own control-total check.</response>
    [HttpPost]
    [HasPermission(Permissions.Payroll.Read)]
    [ProducesResponseType(typeof(BankFileExportResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromBody] ExportBankFileCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);
}
