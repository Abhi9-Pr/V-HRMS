using FluentValidation;

namespace Vespera.Application.Features.Rosters;

public sealed class OverrideRosterAssignmentCommandValidator : AbstractValidator<OverrideRosterAssignmentCommand>
{
    public OverrideRosterAssignmentCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.ShiftId).NotEmpty();
    }
}
