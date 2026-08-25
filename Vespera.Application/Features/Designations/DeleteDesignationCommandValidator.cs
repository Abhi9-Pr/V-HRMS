using FluentValidation;

namespace Vespera.Application.Features.Designations;

public sealed class DeleteDesignationCommandValidator : AbstractValidator<DeleteDesignationCommand>
{
    public DeleteDesignationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
