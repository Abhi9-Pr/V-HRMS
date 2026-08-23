using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class ClearPunchFlagCommandValidator : AbstractValidator<ClearPunchFlagCommand>
{
    public ClearPunchFlagCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.PunchId).NotEmpty();
    }
}
