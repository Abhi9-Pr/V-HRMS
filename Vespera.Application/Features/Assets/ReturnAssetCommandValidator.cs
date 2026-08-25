using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class ReturnAssetCommandValidator : AbstractValidator<ReturnAssetCommand>
{
    public ReturnAssetCommandValidator()
    {
        RuleFor(command => command.AssignmentId).NotEmpty();
        RuleFor(command => command.Condition).NotEmpty().MaximumLength(1024);
    }
}
