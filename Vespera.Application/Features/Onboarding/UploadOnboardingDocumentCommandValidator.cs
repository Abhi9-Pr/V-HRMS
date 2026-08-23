using FluentValidation;

namespace Vespera.Application.Features.Onboarding;

public sealed class UploadOnboardingDocumentCommandValidator : AbstractValidator<UploadOnboardingDocumentCommand>
{
    public UploadOnboardingDocumentCommandValidator()
    {
        RuleFor(command => command.OnboardingDraftId).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Content).NotEmpty();
    }
}
