using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Payroll;

namespace Vespera.Api.Controllers.V1.Finance;

/// <summary>Only the PF ECR extract is implemented in this pass - see
/// <see cref="GetPfEcrReportQuery"/>'s remarks for why, and for the representative-not-certified
/// caveat. ESI return, PT statement, and Form 16 data extract are not yet wired.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/statutory-reports")]
public sealed class StatutoryReportsController : FinanceControllerBase
{
    private readonly ISender _sender;

    public StatutoryReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">A representative PF ECR extract for the run.</response>
    [HttpGet("payroll-runs/{payrollRunId:guid}/pf-ecr")]
    [HasPermission(Permissions.Payroll.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<PfEcrReportLineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPfEcrReport(Guid payrollRunId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetPfEcrReportQuery(payrollRunId), cancellationToken)).ToActionResult(this);
}
