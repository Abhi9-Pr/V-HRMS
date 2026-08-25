using FluentValidation;

namespace Vespera.Application.Features.Departments;

public sealed class DeleteDepartmentCommandValidator : AbstractValidator<DeleteDepartmentCommand>
{
    public DeleteDepartmentCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
