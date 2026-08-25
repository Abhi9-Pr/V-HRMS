using FluentValidation;

namespace Vespera.Application.Features.Locations;

public sealed class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
