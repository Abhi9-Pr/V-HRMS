using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class DecideRequisitionApprovalCommandValidator : AbstractValidator<DecideRequisitionApprovalCommand>
{
    public DecideRequisitionApprovalCommandValidator()
    {
        RuleFor(command => command.RequisitionId).NotEmpty();
    }
}
