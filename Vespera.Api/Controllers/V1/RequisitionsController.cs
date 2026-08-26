using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recruitment/requisitions")]
public sealed class RequisitionsController : ControllerBase
{
    private readonly ISender _sender;

    public RequisitionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new requisition's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Recruitment.ManageRequisitions)]
    [ProducesResponseType(typeof(CreateRequisitionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRequisitionRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new CreateJobRequisitionCommand(request.Title, request.DepartmentId, request.OpeningsCount, idempotencyKey), cancellationToken))
            .ToActionResult(this, id => Ok(new CreateRequisitionResponse(id)));

    /// <response code="204">Stage added.</response>
    [HttpPost("{requisitionId:guid}/stages")]
    [HasPermission(Permissions.Recruitment.ManageRequisitions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddStage(
        Guid requisitionId, [FromBody] AddStageRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new AddPipelineStageCommand(requisitionId, request.StageName, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Submitted for approval.</response>
    [HttpPost("{requisitionId:guid}/submit")]
    [HasPermission(Permissions.Recruitment.ManageRequisitions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitForApproval(
        Guid requisitionId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new SubmitRequisitionForApprovalCommand(requisitionId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Decision recorded.</response>
    [HttpPost("{requisitionId:guid}/decision")]
    [HasPermission(Permissions.Recruitment.ApproveRequisitions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DecideApproval(
        Guid requisitionId, [FromBody] DecideRequisitionApprovalRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new DecideRequisitionApprovalCommand(requisitionId, request.Approved, request.Comment, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Published.</response>
    [HttpPost("{requisitionId:guid}/publish")]
    [HasPermission(Permissions.Recruitment.ManageRequisitions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish(
        Guid requisitionId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new PublishRequisitionCommand(requisitionId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="200">A page of requisitions.</response>
    [HttpGet]
    [HasPermission(Permissions.Recruitment.ManageRequisitions)]
    [ProducesResponseType(typeof(PagedResult<JobRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetJobRequisitionsQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="200">The requisition.</response>
    /// <response code="404">No such requisition.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Recruitment.ManageRequisitions)]
    [ProducesResponseType(typeof(JobRequisitionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetJobRequisitionByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <response code="200">The candidate Kanban pipeline for this requisition.</response>
    [HttpGet("{requisitionId:guid}/pipeline")]
    [HasPermission(Permissions.Recruitment.ManageCandidates)]
    [ProducesResponseType(typeof(CandidatePipelineDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipeline(Guid requisitionId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetCandidatePipelineQuery(requisitionId), cancellationToken)).ToActionResult(this);
}

public sealed record CreateRequisitionResponse(Guid Id);

public sealed record CreateRequisitionRequest(string Title, Guid DepartmentId, int OpeningsCount);

public sealed record AddStageRequest(string StageName);

public sealed record DecideRequisitionApprovalRequest(bool Approved, string? Comment);
