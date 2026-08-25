using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class CreateJobRequisitionCommandValidator : AbstractValidator<CreateJobRequisitionCommand>
{
    public CreateJobRequisitionCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(256);
        RuleFor(command => command.DepartmentId).NotEmpty();
        RuleFor(command => command.OpeningsCount).GreaterThan(0);
    }
}
