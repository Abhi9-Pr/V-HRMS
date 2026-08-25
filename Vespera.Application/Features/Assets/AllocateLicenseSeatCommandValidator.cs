using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class AllocateLicenseSeatCommandValidator : AbstractValidator<AllocateLicenseSeatCommand>
{
    public AllocateLicenseSeatCommandValidator()
    {
        RuleFor(command => command.LicenseId).NotEmpty();
        RuleFor(command => command.EmployeeId).NotEmpty();
    }
}
