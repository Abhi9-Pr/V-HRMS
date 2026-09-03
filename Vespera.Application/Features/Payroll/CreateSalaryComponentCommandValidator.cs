using FluentValidation;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class CreateSalaryComponentCommandValidator : AbstractValidator<CreateSalaryComponentCommand>
{
    public CreateSalaryComponentCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ComponentType)
            .Must(value => Enum.TryParse<SalaryComponentType>(value, out _))
            .WithMessage($"componentType must be one of: {string.Join(", ", Enum.GetNames<SalaryComponentType>())}.");
    }
}
