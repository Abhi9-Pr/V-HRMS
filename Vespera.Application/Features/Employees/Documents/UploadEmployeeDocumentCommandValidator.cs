using FluentValidation;

namespace Vespera.Application.Features.Employees.Documents;

public sealed class UploadEmployeeDocumentCommandValidator : AbstractValidator<UploadEmployeeDocumentCommand>
{
    private const int MaxContentLengthBytes = 20 * 1024 * 1024;

    public UploadEmployeeDocumentCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.DocumentType).IsInEnum();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Content)
            .Must(content => content.Length > 0)
            .WithMessage("File content must not be empty.")
            .Must(content => content.Length <= MaxContentLengthBytes)
            .WithMessage($"File content must not exceed {MaxContentLengthBytes / (1024 * 1024)} MB.");
    }
}
