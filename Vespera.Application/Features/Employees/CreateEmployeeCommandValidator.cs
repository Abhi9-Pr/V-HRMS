using FluentValidation;

namespace Vespera.Application.Features.Employees;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.WorkEmail).NotEmpty().MaximumLength(254);
        RuleFor(command => command.Phone).NotEmpty();
        RuleFor(command => command.DateOfBirth).LessThan(command => command.DateOfJoining);
        RuleFor(command => command.DepartmentId).NotEmpty();
        RuleFor(command => command.DesignationId).NotEmpty();
        RuleFor(command => command.LocationId).NotEmpty();
    }
}
