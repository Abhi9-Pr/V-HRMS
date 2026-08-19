using FluentValidation;

namespace Vespera.Application.Features.Departments;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Code).NotEmpty().MaximumLength(20);
    }
}
