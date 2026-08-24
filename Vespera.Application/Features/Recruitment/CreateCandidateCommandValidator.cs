using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class CreateCandidateCommandValidator : AbstractValidator<CreateCandidateCommand>
{
    public CreateCandidateCommandValidator()
    {
        RuleFor(command => command.JobRequisitionId).NotEmpty();
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Email).NotEmpty().MaximumLength(254);
        RuleFor(command => command.Phone).NotEmpty().MaximumLength(20);
    }
}
