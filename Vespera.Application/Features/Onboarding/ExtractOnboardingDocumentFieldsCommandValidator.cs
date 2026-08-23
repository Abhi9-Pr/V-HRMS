using FluentValidation;

namespace Vespera.Application.Features.Onboarding;

public sealed class ExtractOnboardingDocumentFieldsCommandValidator : AbstractValidator<ExtractOnboardingDocumentFieldsCommand>
{
    public ExtractOnboardingDocumentFieldsCommandValidator()
    {
        RuleFor(command => command.OnboardingDraftId).NotEmpty();
        RuleFor(command => command.EmployeeDocumentId).NotEmpty();
    }
}
