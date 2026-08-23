using FluentValidation;

namespace Vespera.Application.Features.Rosters;

public sealed class PublishRosterCommandValidator : AbstractValidator<PublishRosterCommand>
{
    public PublishRosterCommandValidator()
    {
        RuleFor(command => command.EmployeeIds).NotEmpty();
        RuleFor(command => command.RangeEnd).GreaterThanOrEqualTo(command => command.RangeStart);
    }
}
