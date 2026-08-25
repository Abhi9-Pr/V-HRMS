using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetCandidatePipelineQueryValidator : AbstractValidator<GetCandidatePipelineQuery>
{
    public GetCandidatePipelineQueryValidator()
    {
        RuleFor(query => query.JobRequisitionId).NotEmpty();
    }
}
