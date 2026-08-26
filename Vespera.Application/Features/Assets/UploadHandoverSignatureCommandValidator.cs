using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class UploadHandoverSignatureCommandValidator : AbstractValidator<UploadHandoverSignatureCommand>
{
    public UploadHandoverSignatureCommandValidator()
    {
        RuleFor(command => command.AssignmentId).NotEmpty();
        RuleFor(command => command.Content).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(256);
    }
}
