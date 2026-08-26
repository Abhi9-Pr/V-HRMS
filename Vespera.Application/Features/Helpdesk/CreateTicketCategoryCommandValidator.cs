using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class CreateTicketCategoryCommandValidator : AbstractValidator<CreateTicketCategoryCommand>
{
    public CreateTicketCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(256);
        RuleFor(command => command.DepartmentId).NotEmpty();
    }
}
