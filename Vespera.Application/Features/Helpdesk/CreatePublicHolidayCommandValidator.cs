using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class CreatePublicHolidayCommandValidator : AbstractValidator<CreatePublicHolidayCommand>
{
    public CreatePublicHolidayCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(256);
    }
}
