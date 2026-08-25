using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetJobRequisitionByIdQueryValidator : AbstractValidator<GetJobRequisitionByIdQuery>
{
    public GetJobRequisitionByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
