using FluentValidation;

namespace Vespera.Application.Features.Onboarding;

public sealed class SubmitOnboardingCommandValidator : AbstractValidator<SubmitOnboardingCommand>
{
    public SubmitOnboardingCommandValidator()
    {
        RuleFor(command => command.OnboardingDraftId).NotEmpty();
        RuleFor(command => command.EmployeeCode).NotEmpty().MaximumLength(32);
    }
}
