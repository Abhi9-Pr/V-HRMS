using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

/// <summary>Only the fields the caller actually supplies here get applied to the draft — never
/// the raw <see cref="EmployeeDocument.OcrSuggestedFieldsJson"/> the caller may be looking at on
/// screen. A field left null here is left untouched on the draft, whatever OCR guessed for it.</summary>
public sealed record ConfirmOnboardingDocumentFieldsCommand(
    Guid OnboardingDraftId,
    Guid EmployeeDocumentId,
    string? ConfirmedFirstName,
    string? ConfirmedLastName,
    DateOnly? ConfirmedDateOfBirth) : IRequest<Result>;
