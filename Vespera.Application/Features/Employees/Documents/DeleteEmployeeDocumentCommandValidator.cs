using FluentValidation;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class DeleteEmployeeDocumentCommandValidator : AbstractValidator<DeleteEmployeeDocumentCommand>
{
    public DeleteEmployeeDocumentCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.DocumentId).NotEmpty();
    }
}
