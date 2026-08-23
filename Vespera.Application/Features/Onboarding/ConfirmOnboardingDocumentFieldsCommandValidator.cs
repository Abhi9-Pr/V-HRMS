using FluentValidation;

namespace Vespera.Application.Features.Onboarding;

public sealed class ConfirmOnboardingDocumentFieldsCommandValidator : AbstractValidator<ConfirmOnboardingDocumentFieldsCommand>
{
    public ConfirmOnboardingDocumentFieldsCommandValidator()
    {
        RuleFor(command => command.OnboardingDraftId).NotEmpty();
        RuleFor(command => command.EmployeeDocumentId).NotEmpty();
    }
}
