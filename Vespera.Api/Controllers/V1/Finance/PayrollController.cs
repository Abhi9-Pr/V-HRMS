using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Payroll;

namespace Vespera.Api.Controllers.V1.Finance;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/payroll-runs")]
public sealed class PayrollController : FinanceControllerBase
{
    private readonly ISender _sender;
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly IAuthorizationService _authorizationService;

    public PayrollController(ISender sender, IReadRepository<PayrollRun> payrollRuns, IAuthorizationService authorizationService)
    {
        _sender = sender;
        _payrollRuns = payrollRuns;
        _authorizationService = authorizationService;
    }

    /// <response code="200">The new (Draft) payroll run's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Payroll.Write)]
    public async Task<IActionResult> Open([FromBody] OpenPayrollRunCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new { id }));

    /// <summary>Maker-checker: requires Payroll.Finalize (on top of the Finance.Admin wall) and
    /// that the caller did not create this payroll run.</summary>
    /// <response code="204">Finalized.</response>
    /// <response code="403">The caller lacks Payroll.Finalize, lacks Finance.Admin, or created this run.</response>
    /// <response code="404">No such payroll run.</response>
    [HttpPost("{id:guid}/finalize")]
    [HasPermission(Permissions.Payroll.Finalize)]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken cancellationToken)
    {
        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(new PayrollRunByIdSpecification(new PayrollRunId(id)), cancellationToken);
        if (payrollRun is null)
        {
            return NotFound();
        }

        var authorization = await _authorizationService.AuthorizeAsync(User, payrollRun, new NotCreatorRequirement());
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        var result = await _sender.Send(new FinalizePayrollRunCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
