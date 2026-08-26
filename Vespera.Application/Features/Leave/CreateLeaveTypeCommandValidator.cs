using FluentValidation;

namespace Vespera.Application.Features.Leave;

public sealed class CreateLeaveTypeCommandValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.CarryForwardLimit).GreaterThanOrEqualTo(0);
    }
}
