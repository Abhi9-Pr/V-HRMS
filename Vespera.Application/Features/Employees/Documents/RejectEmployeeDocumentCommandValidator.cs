using FluentValidation;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class RejectEmployeeDocumentCommandValidator : AbstractValidator<RejectEmployeeDocumentCommand>
{
    public RejectEmployeeDocumentCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.DocumentId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1024);
    }
}
