using FluentValidation;

namespace Vespera.Application.Features.Holidays;

public sealed class DeleteHolidayCommandValidator : AbstractValidator<DeleteHolidayCommand>
{
    public DeleteHolidayCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
