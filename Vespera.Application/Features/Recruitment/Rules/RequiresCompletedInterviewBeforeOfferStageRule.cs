using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment.Rules;

/// <summary>A candidate cannot move into a stage whose name suggests an offer decision (e.g.
/// "Offer") without at least one completed interview on record.</summary>
public sealed class RequiresCompletedInterviewBeforeOfferStageRule : IStageTransitionRule
{
    public int Order => 1;

    public IReadOnlyList<string> Evaluate(
        Candidate candidate, PipelineStageId targetStageId, IReadOnlyList<PipelineStage> requisitionStages,
        IReadOnlyList<Interview> candidateInterviews)
    {
        var targetStage = requisitionStages.FirstOrDefault(stage => stage.Id == targetStageId);
        if (targetStage is null || !targetStage.Name.Contains("offer", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var hasCompletedInterview = candidateInterviews.Any(interview => interview.Status == InterviewStatus.Completed);
        return hasCompletedInterview
            ? []
            : [$"Cannot move to '{targetStage.Name}' before at least one interview has been completed."];
    }
}
