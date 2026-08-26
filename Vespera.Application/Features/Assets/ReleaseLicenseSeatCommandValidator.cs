using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class ReleaseLicenseSeatCommandValidator : AbstractValidator<ReleaseLicenseSeatCommand>
{
    public ReleaseLicenseSeatCommandValidator()
    {
        RuleFor(command => command.AllocationId).NotEmpty();
    }
}
