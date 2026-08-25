using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class CompleteOffboardingChecklistItemCommandValidator : AbstractValidator<CompleteOffboardingChecklistItemCommand>
{
    public CompleteOffboardingChecklistItemCommandValidator()
    {
        RuleFor(command => command.ChecklistId).NotEmpty();
        RuleFor(command => command.ItemIndex).GreaterThanOrEqualTo(0);
    }
}
