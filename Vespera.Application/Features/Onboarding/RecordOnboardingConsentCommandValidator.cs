using FluentValidation;

namespace Vespera.Application.Features.Onboarding;

public sealed class RecordOnboardingConsentCommandValidator : AbstractValidator<RecordOnboardingConsentCommand>
{
    public RecordOnboardingConsentCommandValidator()
    {
        RuleFor(command => command.OnboardingDraftId).NotEmpty();
        RuleFor(command => command.ConsentType).NotEmpty();
    }
}
