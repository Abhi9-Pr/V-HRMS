using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class PublishRequisitionCommandValidator : AbstractValidator<PublishRequisitionCommand>
{
    public PublishRequisitionCommandValidator()
    {
        RuleFor(command => command.RequisitionId).NotEmpty();
    }
}
