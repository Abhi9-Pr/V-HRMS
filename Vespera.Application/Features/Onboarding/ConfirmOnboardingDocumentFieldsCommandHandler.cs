using System.Text.Json;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed class ConfirmOnboardingDocumentFieldsCommandHandler : IRequestHandler<ConfirmOnboardingDocumentFieldsCommand, Result>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmOnboardingDocumentFieldsCommandHandler(
        IReadRepository<OnboardingDraft> drafts,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _drafts = drafts;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ConfirmOnboardingDocumentFieldsCommand request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftByIdSpecification(_tenantContext.TenantId, new OnboardingDraftId(request.OnboardingDraftId));
        var draft = await _drafts.FirstOrDefaultAsync(specification, cancellationToken);
        if (draft is null)
        {
            return Result.Failure(Error.NotFound("onboarding_draft.not_found", "Onboarding draft not found."));
        }

        var document = draft.Documents.FirstOrDefault(d => d.Id == new EmployeeDocumentId(request.EmployeeDocumentId));
        if (document is null)
        {
            return Result.Failure(Error.NotFound("employee_document.not_found", "Document not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var confirmedBy = _currentUser.UserId?.ToString() ?? "system";

        var confirmedFields = new Dictionary<string, string>();
        if (request.ConfirmedFirstName is not null)
        {
            confirmedFields["firstName"] = request.ConfirmedFirstName;
        }

        if (request.ConfirmedLastName is not null)
        {
            confirmedFields["lastName"] = request.ConfirmedLastName;
        }

        if (request.ConfirmedDateOfBirth is not null)
        {
            confirmedFields["dateOfBirth"] = request.ConfirmedDateOfBirth.Value.ToString("O");
        }

        var confirmResult = document.ConfirmOcrSuggestion(JsonSerializer.Serialize(confirmedFields), confirmedBy, now);
        if (confirmResult.IsFailure)
        {
            return confirmResult;
        }

        var hasConfirmedPersonalDetails =
            request.ConfirmedFirstName is not null || request.ConfirmedLastName is not null || request.ConfirmedDateOfBirth is not null;
        if (!hasConfirmedPersonalDetails)
        {
            return Result.Success();
        }

        // The draft must already have a personal-details snapshot (email/phone) to merge these
        // confirmed values into — a document can be uploaded before the personal-details step is
        // reached, but confirming OCR-suggested identity fields onto the draft can't happen until
        // there's a snapshot to merge them into.
        if (draft.WorkEmail is null || draft.Phone is null)
        {
            return Result.Failure(Error.Validation(
                "onboarding_draft.personal_details_not_started",
                "Complete the personal details step before confirming document fields."));
        }

        var firstName = request.ConfirmedFirstName ?? draft.FirstName ?? string.Empty;
        var lastName = request.ConfirmedLastName ?? draft.LastName ?? string.Empty;
        var dateOfBirth = request.ConfirmedDateOfBirth ?? draft.DateOfBirth ?? default;

        return draft.UpdatePersonalDetails(firstName, lastName, draft.WorkEmail, draft.Phone, dateOfBirth, now, confirmedBy);
    }
}
