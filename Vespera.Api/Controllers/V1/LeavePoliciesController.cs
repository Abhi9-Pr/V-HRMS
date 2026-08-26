using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Leave;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-policies")]
public sealed class LeavePoliciesController : ControllerBase
{
    private readonly ISender _sender;

    public LeavePoliciesController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">Every leave policy configured for the tenant.</response>
    [HttpGet]
    [HasPermission(Permissions.Leave.ManagePolicy)]
    [ProducesResponseType(typeof(IReadOnlyList<LeavePolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLeavePolicies(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetLeavePoliciesQuery(), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new policy's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Leave.ManagePolicy)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateLeavePolicy(CreateLeavePolicyCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <response code="204">Accrual, approval-chain, and balance settings updated.</response>
    /// <response code="404">No such policy.</response>
    [HttpPut("{leavePolicyId:guid}/settings")]
    [HasPermission(Permissions.Leave.ManagePolicy)]
    public async Task<IActionResult> UpdateSettings(
        Guid leavePolicyId, UpdateLeavePolicySettingsBody body, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new UpdateLeavePolicySettingsCommand(
                leavePolicyId, body.AccrualFrequency, body.MinimumTenureMonthsForAccrual, body.RequiresSkipLevelApproval,
                body.SkipLevelThresholdDays, body.RequiresHrApproval, body.NegativeBalancePolicy, body.MaxNegativeBalanceDays,
                body.SandwichLeaveEnabled),
            cancellationToken)).ToActionResult(this);
}

public sealed record UpdateLeavePolicySettingsBody(
    string AccrualFrequency,
    int MinimumTenureMonthsForAccrual,
    bool RequiresSkipLevelApproval,
    decimal? SkipLevelThresholdDays,
    bool RequiresHrApproval,
    string NegativeBalancePolicy,
    decimal MaxNegativeBalanceDays,
    bool SandwichLeaveEnabled);
