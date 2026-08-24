using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetCandidatePipelineQueryHandler : IRequestHandler<GetCandidatePipelineQuery, Result<CandidatePipelineDto>>
{
    private readonly IReadRepository<JobRequisition> _requisitions;
    private readonly IReadRepository<Candidate> _candidates;
    private readonly ITenantContext _tenantContext;

    public GetCandidatePipelineQueryHandler(
        IReadRepository<JobRequisition> requisitions, IReadRepository<Candidate> candidates, ITenantContext tenantContext)
    {
        _requisitions = requisitions;
        _candidates = candidates;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CandidatePipelineDto>> Handle(GetCandidatePipelineQuery request, CancellationToken cancellationToken)
    {
        var requisition = await _requisitions.FirstOrDefaultAsync(
            new JobRequisitionByIdSpecification(new JobRequisitionId(request.JobRequisitionId)), cancellationToken);

        if (requisition is null)
        {
            return Result.Failure<CandidatePipelineDto>(Error.NotFound("job_requisition.not_found", "Job requisition not found."));
        }

        var candidates = await _candidates.ListAsync(
            new CandidatesByRequisitionSpecification(_tenantContext.TenantId, requisition.Id), cancellationToken);

        var stages = requisition.Stages.OrderBy(stage => stage.SequenceNumber).ToList().Adapt<List<PipelineStageDto>>();
        var cards = candidates.Adapt<List<CandidateCardDto>>();

        return Result.Success(new CandidatePipelineDto(stages, cards));
    }
}
