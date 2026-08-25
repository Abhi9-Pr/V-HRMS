using FluentValidation;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class VerifyEmployeeDocumentCommandValidator : AbstractValidator<VerifyEmployeeDocumentCommand>
{
    public VerifyEmployeeDocumentCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.DocumentId).NotEmpty();
    }
}
