using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Eis;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/onboarding")]
public sealed class OnboardingController : ControllerBase
{
    private readonly ISender _sender;

    public OnboardingController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">A page of onboarding drafts.</response>
    [HttpGet]
    [HasPermission(Permissions.Onboarding.Read)]
    [ProducesResponseType(typeof(PagedResult<OnboardingDraftSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetOnboardingDraftsQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="200">The draft.</response>
    /// <response code="404">No such draft.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Onboarding.Read)]
    [ProducesResponseType(typeof(OnboardingDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetOnboardingDraftByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new draft's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Onboarding.Manage)]
    [ProducesResponseType(typeof(StartOnboardingDraftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Start(CancellationToken cancellationToken) =>
        (await _sender.Send(new StartOnboardingDraftCommand(), cancellationToken))
            .ToActionResult(this, id => Ok(new StartOnboardingDraftResponse(id)));

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such draft.</response>
    [HttpPut("{id:guid}/personal-details")]
    [HasPermission(Permissions.Onboarding.Manage)]
    public async Task<IActionResult> UpdatePersonalDetails(
        Guid id, UpdatePersonalDetailsRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new UpdateOnboardingPersonalDetailsCommand(id, request.FirstName, request.LastName, request.WorkEmail, request.Phone, request.DateOfBirth),
            cancellationToken)).ToActionResult(this);

    /// <response code="204">Updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such draft.</response>
    [HttpPut("{id:guid}/employment-details")]
    [HasPermission(Permissions.Onboarding.Manage)]
    public async Task<IActionResult> UpdateEmploymentDetails(
        Guid id, UpdateEmploymentDetailsRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new UpdateOnboardingEmploymentDetailsCommand(id, request.DepartmentId, request.DesignationId, request.LocationId, request.DateOfJoining),
            cancellationToken)).ToActionResult(this);

    /// <summary>The file is virus-scanned before it is ever written to storage — an infected file
    /// is rejected outright, never stored and never attached to the draft.</summary>
    /// <response code="200">The new document's id.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such draft.</response>
    /// <response code="409">The file failed its virus scan and was not stored.</response>
    [HttpPost("{id:guid}/documents")]
    [HasPermission(Permissions.Onboarding.Manage)]
    [ProducesResponseType(typeof(UploadOnboardingDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadDocument(
        Guid id, [FromForm] UploadOnboardingDocumentRequest request, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);

        var command = new UploadOnboardingDocumentCommand(id, request.DocumentType, request.File.FileName, stream.ToArray());
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, documentId => Ok(new UploadOnboardingDocumentResponse(documentId)));
    }

    /// <summary>Runs OCR over an already-uploaded document and returns the result as a
    /// suggestion only — nothing about calling this endpoint changes any draft data. Requires a
    /// granted <c>DataProcessing</c> consent to already be on record; see
    /// <c>ExtractOnboardingDocumentFieldsCommandHandler</c>.</summary>
    /// <response code="200">The OCR suggestion.</response>
    /// <response code="403">No data-processing consent is on record yet.</response>
    /// <response code="404">No such draft or document.</response>
    [HttpPost("{id:guid}/documents/{documentId:guid}/extract")]
    [HasPermission(Permissions.Onboarding.Manage)]
    [ProducesResponseType(typeof(OcrExtractionResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExtractDocumentFields(Guid id, Guid documentId, CancellationToken cancellationToken) =>
        (await _sender.Send(new ExtractOnboardingDocumentFieldsCommand(id, documentId), cancellationToken)).ToActionResult(this);

    /// <summary>Applies only the fields the caller explicitly confirms here — never the raw OCR
    /// suggestion — to the draft.</summary>
    /// <response code="204">Confirmed.</response>
    /// <response code="400">Validation failed, or personal details haven't been started yet.</response>
    /// <response code="404">No such draft or document.</response>
    [HttpPost("{id:guid}/documents/{documentId:guid}/confirm")]
    [HasPermission(Permissions.Onboarding.Manage)]
    public async Task<IActionResult> ConfirmDocumentFields(
        Guid id, Guid documentId, ConfirmDocumentFieldsRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new ConfirmOnboardingDocumentFieldsCommand(id, documentId, request.ConfirmedFirstName, request.ConfirmedLastName, request.ConfirmedDateOfBirth),
            cancellationToken)).ToActionResult(this);

    /// <response code="204">Recorded.</response>
    /// <response code="404">No such draft.</response>
    [HttpPost("{id:guid}/consent")]
    [HasPermission(Permissions.Onboarding.Manage)]
    public async Task<IActionResult> RecordConsent(Guid id, RecordConsentRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new RecordOnboardingConsentCommand(id, request.ConsentType), cancellationToken)).ToActionResult(this);

    /// <summary>Validates completeness and, if the draft passes, converts it into a real
    /// <see cref="Vespera.Domain.Eis.Employee"/> in the same step.</summary>
    /// <response code="200">The new employee's id.</response>
    /// <response code="400">The draft is incomplete.</response>
    /// <response code="404">No such draft.</response>
    /// <response code="409">The draft has already been submitted or converted.</response>
    [HttpPost("{id:guid}/submit")]
    [HasPermission(Permissions.Onboarding.Manage)]
    [ProducesResponseType(typeof(SubmitOnboardingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit(Guid id, SubmitOnboardingRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new SubmitOnboardingCommand(id, request.EmployeeCode), cancellationToken))
            .ToActionResult(this, employeeId => Ok(new SubmitOnboardingResponse(employeeId)));
}

public sealed record StartOnboardingDraftResponse(Guid Id);

public sealed record UpdatePersonalDetailsRequest(string FirstName, string LastName, string WorkEmail, string Phone, DateOnly DateOfBirth);

public sealed record UpdateEmploymentDetailsRequest(Guid DepartmentId, Guid DesignationId, Guid LocationId, DateOnly DateOfJoining);

public sealed record UploadOnboardingDocumentRequest(EmployeeDocumentType DocumentType, IFormFile File);

public sealed record UploadOnboardingDocumentResponse(Guid Id);

public sealed record ConfirmDocumentFieldsRequest(string? ConfirmedFirstName, string? ConfirmedLastName, DateOnly? ConfirmedDateOfBirth);

public sealed record RecordConsentRequest(string ConsentType);

public sealed record SubmitOnboardingRequest(string EmployeeCode);

public sealed record SubmitOnboardingResponse(Guid EmployeeId);
