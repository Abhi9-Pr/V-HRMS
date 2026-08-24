using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment.Rules;

/// <summary>One evaluable rule of the candidate stage-transition engine. OCP: add a new rule by
/// adding a new implementation and one DI registration line — never by editing an existing
/// rule. Unlike the Expense policy engine there is no warn/block severity split — any non-empty
/// result blocks the move.</summary>
public interface IStageTransitionRule
{
    public int Order { get; }

    public IReadOnlyList<string> Evaluate(
        Candidate candidate, PipelineStageId targetStageId, IReadOnlyList<PipelineStage> requisitionStages,
        IReadOnlyList<Interview> candidateInterviews);
}
