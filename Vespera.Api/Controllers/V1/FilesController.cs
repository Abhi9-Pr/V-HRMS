using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Storage;

namespace Vespera.Api.Controllers.V1;

/// <summary>
/// Anonymous-but-signature-checked download callback for <see cref="LocalFileStorage"/>'s
/// self-signed URLs (see <c>LocalFileStorage.GetDownloadUrlAsync</c>) — the actual
/// permission/scan-status check happens once, at the point a signed URL is issued
/// (e.g. <c>GetEmployeeDocumentDownloadUrlQuery</c>), not here; this endpoint only verifies the
/// signature and expiry baked into the URL itself. A future object-store provider would return
/// its own native pre-signed URL instead and this controller would have no callers.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileStorage _fileStorage;
    private readonly LocalFileStorage _localFileStorage;

    public FilesController(IFileStorage fileStorage, LocalFileStorage localFileStorage)
    {
        _fileStorage = fileStorage;
        _localFileStorage = localFileStorage;
    }

    /// <response code="200">The file content.</response>
    /// <response code="403">The signature is invalid or has expired.</response>
    [HttpGet("{storageKey}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(
        string storageKey,
        [FromQuery] long expires,
        [FromQuery] string sig,
        CancellationToken cancellationToken)
    {
        if (!_localFileStorage.ValidateSignature(storageKey, expires, sig))
        {
            // Not Forbid() — this endpoint is anonymous, so there's no auth challenge scheme to
            // invoke; a plain 403 is the correct response to an invalid/expired signature.
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var stream = await _fileStorage.DownloadAsync(storageKey, cancellationToken);
        return File(stream, "application/octet-stream", storageKey);
    }
}
