using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetCandidateByIdQueryValidator : AbstractValidator<GetCandidateByIdQuery>
{
    public GetCandidateByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
