using FluentValidation;

namespace Vespera.Application.Features.Eis;

public sealed class EndReportingRelationshipCommandValidator : AbstractValidator<EndReportingRelationshipCommand>
{
    public EndReportingRelationshipCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.Id).NotEmpty();
    }
}
