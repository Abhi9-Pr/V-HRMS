using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Employees.Documents;
using Vespera.Domain.Eis;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees/{employeeId:guid}/documents")]
public sealed class EmployeeDocumentsController : ControllerBase
{
    private readonly ISender _sender;

    public EmployeeDocumentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The employee's documents.</response>
    /// <response code="404">No such employee.</response>
    [HttpGet]
    [HasPermission(Permissions.EmployeeDocuments.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid employeeId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetEmployeeDocumentsQuery(employeeId), cancellationToken)).ToActionResult(this);

    /// <summary>A time-limited signed download link — see <c>IFileStorage.GetDownloadUrlAsync</c>.</summary>
    /// <response code="200">The download URL.</response>
    /// <response code="404">No such employee or document.</response>
    /// <response code="409">The document hasn't cleared its virus scan yet.</response>
    [HttpGet("{documentId:guid}/download-url")]
    [HasPermission(Permissions.EmployeeDocuments.Read)]
    [ProducesResponseType(typeof(DocumentDownloadUrlDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDownloadUrl(Guid employeeId, Guid documentId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetEmployeeDocumentDownloadUrlQuery(employeeId, documentId), cancellationToken)).ToActionResult(this);

    /// <summary>The file is virus-scanned before it is ever written to storage — an infected file
    /// is rejected outright, never stored and never attached to the employee.</summary>
    /// <response code="200">The new document's id.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such employee.</response>
    /// <response code="409">The file failed its virus scan and was not stored.</response>
    [HttpPost]
    [HasPermission(Permissions.EmployeeDocuments.Manage)]
    [ProducesResponseType(typeof(UploadEmployeeDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upload(
        Guid employeeId, [FromForm] UploadEmployeeDocumentRequest request, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);

        var command = new UploadEmployeeDocumentCommand(employeeId, request.DocumentType, request.File.FileName, stream.ToArray());
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new UploadEmployeeDocumentResponse(id)));
    }

    /// <response code="204">Verified.</response>
    /// <response code="404">No such employee or document.</response>
    /// <response code="409">Already verified.</response>
    [HttpPost("{documentId:guid}/verify")]
    [HasPermission(Permissions.EmployeeDocuments.Manage)]
    public async Task<IActionResult> Verify(Guid employeeId, Guid documentId, CancellationToken cancellationToken) =>
        (await _sender.Send(new VerifyEmployeeDocumentCommand(employeeId, documentId), cancellationToken)).ToActionResult(this);

    /// <response code="204">Rejected.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No such employee or document.</response>
    [HttpPost("{documentId:guid}/reject")]
    [HasPermission(Permissions.EmployeeDocuments.Manage)]
    public async Task<IActionResult> Reject(
        Guid employeeId, Guid documentId, RejectEmployeeDocumentRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new RejectEmployeeDocumentCommand(employeeId, documentId, request.Reason), cancellationToken)).ToActionResult(this);

    /// <response code="204">Deleted.</response>
    /// <response code="404">No such employee or document.</response>
    [HttpDelete("{documentId:guid}")]
    [HasPermission(Permissions.EmployeeDocuments.Manage)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid documentId, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteEmployeeDocumentCommand(employeeId, documentId), cancellationToken)).ToActionResult(this);
}

public sealed record UploadEmployeeDocumentRequest(EmployeeDocumentType DocumentType, IFormFile File);

public sealed record UploadEmployeeDocumentResponse(Guid Id);

public sealed record RejectEmployeeDocumentRequest(string Reason);
