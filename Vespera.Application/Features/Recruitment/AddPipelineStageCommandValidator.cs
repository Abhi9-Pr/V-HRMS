using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class AddPipelineStageCommandValidator : AbstractValidator<AddPipelineStageCommand>
{
    public AddPipelineStageCommandValidator()
    {
        RuleFor(command => command.RequisitionId).NotEmpty();
        RuleFor(command => command.StageName).NotEmpty().MaximumLength(100);
    }
}
