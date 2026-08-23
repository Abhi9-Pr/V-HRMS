using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
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

    /// <response code="200">Opened.</response>
    [HttpPost]
    [HasPermission(Permissions.Payroll.Write)]
    public async Task<IActionResult> Open([FromBody] OpenPayrollRunRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new OpenPayrollRunCommand(request.Month, request.Year, request.IdempotencyKey), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <response code="204">Attendance frozen.</response>
    [HttpPost("{id:guid}/freeze-attendance")]
    [HasPermission(Permissions.Payroll.Write)]
    public async Task<IActionResult> FreezeAttendance(Guid id, [FromBody] FreezeAttendanceRequest? request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new FreezeAttendanceCommand(id, request?.OverrideReason), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <response code="204">Dry-run computed.</response>
    [HttpPost("{id:guid}/dry-run")]
    [HasPermission(Permissions.Payroll.Write)]
    public async Task<IActionResult> RunDryRun(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RunDryRunCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <response code="204">Submitted for review.</response>
    [HttpPost("{id:guid}/submit-for-review")]
    [HasPermission(Permissions.Payroll.Write)]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SubmitPayrollRunForReviewCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <response code="204">Approved.</response>
    [HttpPost("{id:guid}/approve")]
    [HasPermission(Permissions.Payroll.Write)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ApprovePayrollRunCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Maker-checker: requires Payroll.Finalize (on top of the Finance.Admin wall), that
    /// the caller did not create this payroll run, and that the caller did not run its dry-run
    /// compute either — two independent checks, since those can be two different people.</summary>
    /// <response code="204">Finalized.</response>
    /// <response code="403">The caller lacks Payroll.Finalize, lacks Finance.Admin, created this run, or ran its dry-run.</response>
    /// <response code="404">No such payroll run.</response>
    [HttpPost("{id:guid}/finalize")]
    [HasPermission(Permissions.Payroll.Finalize)]
    public async Task<IActionResult> Finalize(Guid id, [FromQuery] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(new PayrollRunByIdSpecification(new PayrollRunId(id)), cancellationToken);
        if (payrollRun is null)
        {
            return NotFound();
        }

        var notCreator = await _authorizationService.AuthorizeAsync(User, payrollRun, new NotCreatorRequirement());
        var notDryRunExecutor = await _authorizationService.AuthorizeAsync(User, payrollRun, new NotDryRunExecutorRequirement());
        if (!notCreator.Succeeded || !notDryRunExecutor.Succeeded)
        {
            return Forbid();
        }

        var result = await _sender.Send(new FinalizePayrollRunCommand(id, idempotencyKey), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <response code="204">Published.</response>
    [HttpPost("{id:guid}/publish")]
    [HasPermission(Permissions.Payroll.Write)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PublishPayrollRunCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <response code="200">The payroll run, with every computed line.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Payroll.Read)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPayrollRunQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <response code="200">A page of payroll run summaries for this tenant.</response>
    [HttpGet]
    [HasPermission(Permissions.Payroll.Read)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPayrollRunsQuery(paging), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <response code="200">Per-employee variance against the prior finalized cycle.</response>
    [HttpGet("{id:guid}/variance")]
    [HasPermission(Permissions.Payroll.Read)]
    public async Task<IActionResult> GetVariance(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPayrollRunVarianceQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }
}

public sealed record OpenPayrollRunRequest(int Month, int Year, string? IdempotencyKey);

public sealed record FreezeAttendanceRequest(string? OverrideReason);
