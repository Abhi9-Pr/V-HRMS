using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetInterviewsForCandidateQueryHandler : IRequestHandler<GetInterviewsForCandidateQuery, Result<IReadOnlyList<InterviewDto>>>
{
    private readonly IReadRepository<Interview> _interviews;
    private readonly ITenantContext _tenantContext;

    public GetInterviewsForCandidateQueryHandler(IReadRepository<Interview> interviews, ITenantContext tenantContext)
    {
        _interviews = interviews;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<InterviewDto>>> Handle(GetInterviewsForCandidateQuery request, CancellationToken cancellationToken)
    {
        var interviews = await _interviews.ListAsync(
            new InterviewsByCandidateSpecification(_tenantContext.TenantId, new CandidateId(request.CandidateId)), cancellationToken);

        return Result.Success<IReadOnlyList<InterviewDto>>(interviews.Adapt<List<InterviewDto>>());
    }
}
