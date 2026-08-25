using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class ConvertCandidateToEmployeeCommandValidator : AbstractValidator<ConvertCandidateToEmployeeCommand>
{
    public ConvertCandidateToEmployeeCommandValidator()
    {
        RuleFor(command => command.CandidateId).NotEmpty();
        RuleFor(command => command.OfferLetterId).NotEmpty();
        RuleFor(command => command.EmployeeCode).NotEmpty().MaximumLength(32);
        RuleFor(command => command.LocationId).NotEmpty();
    }
}
