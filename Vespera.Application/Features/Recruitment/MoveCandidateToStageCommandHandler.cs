using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class MoveCandidateToStageCommandHandler : IRequestHandler<MoveCandidateToStageCommand, Result>
{
    private readonly IReadRepository<Candidate> _candidateReads;
    private readonly IWriteRepository<Candidate> _candidates;
    private readonly IReadRepository<JobRequisition> _requisitions;
    private readonly IReadRepository<Interview> _interviews;
    private readonly StageTransitionEvaluator _stageTransitionEvaluator;

    public MoveCandidateToStageCommandHandler(
        IReadRepository<Candidate> candidateReads, IWriteRepository<Candidate> candidates, IReadRepository<JobRequisition> requisitions,
        IReadRepository<Interview> interviews, StageTransitionEvaluator stageTransitionEvaluator)
    {
        _candidateReads = candidateReads;
        _candidates = candidates;
        _requisitions = requisitions;
        _interviews = interviews;
        _stageTransitionEvaluator = stageTransitionEvaluator;
    }

    public async Task<Result> Handle(MoveCandidateToStageCommand request, CancellationToken cancellationToken)
    {
        var candidate = await _candidateReads.FirstOrDefaultAsync(
            new CandidateByIdSpecification(new CandidateId(request.CandidateId)), cancellationToken);

        if (candidate is null)
        {
            return Result.Failure(Error.NotFound("candidate.not_found", "Candidate not found."));
        }

        var requisition = await _requisitions.FirstOrDefaultAsync(
            new JobRequisitionByIdSpecification(candidate.JobRequisitionId), cancellationToken);

        if (requisition is null)
        {
            return Result.Failure(Error.NotFound("candidate.requisition_not_found", "The candidate's job requisition was not found."));
        }

        var interviews = await _interviews.ListAsync(
            new InterviewsByCandidateSpecification(candidate.TenantId, candidate.Id), cancellationToken);

        var targetStageId = new PipelineStageId(request.TargetStageId);
        var violations = _stageTransitionEvaluator.Evaluate(candidate, targetStageId, requisition.Stages, interviews);
        if (violations.Count > 0)
        {
            return Result.Failure(Error.Validation("candidate.stage_transition_blocked", string.Join(" ", violations)));
        }

        var result = candidate.MoveToStage(targetStageId);
        if (result.IsFailure)
        {
            return result;
        }

        _candidates.Update(candidate);
        return Result.Success();
    }
}
