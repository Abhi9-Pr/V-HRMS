using FluentValidation;

namespace Vespera.Application.Features.Shifts;

public sealed class DeleteShiftCommandValidator : AbstractValidator<DeleteShiftCommand>
{
    public DeleteShiftCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
