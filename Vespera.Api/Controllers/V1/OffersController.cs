using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recruitment/offers")]
public sealed class OffersController : ControllerBase
{
    private readonly ISender _sender;

    public OffersController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new offer's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Recruitment.ManageOffers)]
    [ProducesResponseType(typeof(CreateOfferResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOfferRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(
                new CreateOfferLetterCommand(
                    request.CandidateId, request.ProposedDesignationId, request.ProposedCtc, request.Currency, request.JoiningDate, idempotencyKey),
                cancellationToken))
            .ToActionResult(this, id => Ok(new CreateOfferResponse(id)));

    /// <response code="204">Sent.</response>
    [HttpPost("{offerLetterId:guid}/send")]
    [HasPermission(Permissions.Recruitment.ManageOffers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Send(
        Guid offerLetterId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new SendOfferLetterCommand(offerLetterId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Accepted.</response>
    [HttpPost("{offerLetterId:guid}/accept")]
    [HasPermission(Permissions.Recruitment.ManageOffers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Accept(
        Guid offerLetterId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new AcceptOfferCommand(offerLetterId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Declined.</response>
    [HttpPost("{offerLetterId:guid}/decline")]
    [HasPermission(Permissions.Recruitment.ManageOffers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Decline(
        Guid offerLetterId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeclineOfferCommand(offerLetterId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Withdrawn.</response>
    [HttpPost("{offerLetterId:guid}/withdraw")]
    [HasPermission(Permissions.Recruitment.ManageOffers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Withdraw(
        Guid offerLetterId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new WithdrawOfferCommand(offerLetterId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="200">The offer letter as a PDF.</response>
    [HttpGet("{offerLetterId:guid}/pdf")]
    [HasPermission(Permissions.Recruitment.ManageOffers)]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadPdf(Guid offerLetterId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GenerateOfferLetterPdfQuery(offerLetterId), cancellationToken);
        return result.ToActionResult(this, bytes => File(bytes, "application/pdf", $"offer-{offerLetterId}.pdf"));
    }

    /// <response code="200">The offer letter.</response>
    /// <response code="404">No such offer letter.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Recruitment.ManageOffers)]
    [ProducesResponseType(typeof(OfferLetterDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetOfferLetterByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <response code="200">The candidate's offer letters.</response>
    [HttpGet("by-candidate/{candidateId:guid}")]
    [HasPermission(Permissions.Recruitment.ManageOffers)]
    [ProducesResponseType(typeof(IReadOnlyList<OfferLetterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForCandidate(Guid candidateId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetOfferLettersForCandidateQuery(candidateId), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new employee's id.</response>
    [HttpPost("convert-to-employee")]
    [HasPermission(Permissions.Recruitment.ConvertToEmployee)]
    [ProducesResponseType(typeof(ConvertToEmployeeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConvertToEmployee(
        [FromBody] ConvertToEmployeeRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(
                new ConvertCandidateToEmployeeCommand(
                    request.CandidateId, request.OfferLetterId, request.EmployeeCode, request.DateOfBirth, request.LocationId, idempotencyKey),
                cancellationToken))
            .ToActionResult(this, id => Ok(new ConvertToEmployeeResponse(id)));
}

public sealed record CreateOfferResponse(Guid Id);

public sealed record ConvertToEmployeeResponse(Guid Id);

public sealed record CreateOfferRequest(Guid CandidateId, Guid ProposedDesignationId, decimal ProposedCtc, Currency Currency, DateOnly JoiningDate);

public sealed record ConvertToEmployeeRequest(Guid CandidateId, Guid OfferLetterId, string EmployeeCode, DateOnly DateOfBirth, Guid LocationId);
