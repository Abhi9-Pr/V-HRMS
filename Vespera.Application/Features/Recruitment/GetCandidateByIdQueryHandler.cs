using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetCandidateByIdQueryHandler : IRequestHandler<GetCandidateByIdQuery, Result<CandidateDto>>
{
    private readonly IReadRepository<Candidate> _candidates;

    public GetCandidateByIdQueryHandler(IReadRepository<Candidate> candidates)
    {
        _candidates = candidates;
    }

    public async Task<Result<CandidateDto>> Handle(GetCandidateByIdQuery request, CancellationToken cancellationToken)
    {
        var candidate = await _candidates.FirstOrDefaultAsync(new CandidateByIdSpecification(new CandidateId(request.Id)), cancellationToken);

        return candidate is null
            ? Result.Failure<CandidateDto>(Error.NotFound("candidate.not_found", "Candidate not found."))
            : Result.Success(candidate.Adapt<CandidateDto>());
    }
}
