using FluentValidation;

namespace Vespera.Application.Features.Eis;

public sealed class CreateReportingRelationshipCommandValidator : AbstractValidator<CreateReportingRelationshipCommand>
{
    public CreateReportingRelationshipCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.ManagerId).NotEmpty();
        RuleFor(command => command.ValidTo)
            .GreaterThanOrEqualTo(command => command.ValidFrom)
            .When(command => command.ValidTo is not null)
            .WithMessage("ValidTo cannot be before ValidFrom.");
    }
}
