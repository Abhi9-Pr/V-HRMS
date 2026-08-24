using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class AssignAssetCommandValidator : AbstractValidator<AssignAssetCommand>
{
    public AssignAssetCommandValidator()
    {
        RuleFor(command => command.AssetId).NotEmpty();
        RuleFor(command => command.EmployeeId).NotEmpty();
    }
}
