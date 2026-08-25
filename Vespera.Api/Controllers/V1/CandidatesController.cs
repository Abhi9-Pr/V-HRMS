using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recruitment/candidates")]
public sealed class CandidatesController : ControllerBase
{
    private readonly ISender _sender;

    public CandidatesController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new candidate's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Recruitment.ManageCandidates)]
    [ProducesResponseType(typeof(CreateCandidateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCandidateRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(
                new CreateCandidateCommand(request.JobRequisitionId, request.FullName, request.Email, request.Phone, idempotencyKey),
                cancellationToken))
            .ToActionResult(this, id => Ok(new CreateCandidateResponse(id)));

    /// <response code="204">Moved.</response>
    [HttpPost("{candidateId:guid}/stage")]
    [HasPermission(Permissions.Recruitment.ManageCandidates)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MoveToStage(
        Guid candidateId, [FromBody] MoveToStageRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new MoveCandidateToStageCommand(candidateId, request.TargetStageId, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Rejected.</response>
    [HttpPost("{candidateId:guid}/reject")]
    [HasPermission(Permissions.Recruitment.ManageCandidates)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(
        Guid candidateId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new RejectCandidateCommand(candidateId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Withdrawn.</response>
    [HttpPost("{candidateId:guid}/withdraw")]
    [HasPermission(Permissions.Recruitment.ManageCandidates)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Withdraw(
        Guid candidateId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new WithdrawCandidateCommand(candidateId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="200">The candidate.</response>
    /// <response code="404">No such candidate.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Recruitment.ManageCandidates)]
    [ProducesResponseType(typeof(CandidateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetCandidateByIdQuery(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateCandidateResponse(Guid Id);

public sealed record CreateCandidateRequest(Guid JobRequisitionId, string FullName, string Email, string Phone);

public sealed record MoveToStageRequest(Guid TargetStageId);
