using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class SubmitRequisitionForApprovalCommandValidator : AbstractValidator<SubmitRequisitionForApprovalCommand>
{
    public SubmitRequisitionForApprovalCommandValidator()
    {
        RuleFor(command => command.RequisitionId).NotEmpty();
    }
}
