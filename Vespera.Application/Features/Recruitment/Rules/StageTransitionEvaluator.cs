using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment.Rules;

public sealed class StageTransitionEvaluator
{
    private readonly IReadOnlyList<IStageTransitionRule> _rules;

    public StageTransitionEvaluator(IEnumerable<IStageTransitionRule> rules)
    {
        _rules = rules.OrderBy(rule => rule.Order).ToList();
    }

    public IReadOnlyList<string> Evaluate(
        Candidate candidate, PipelineStageId targetStageId, IReadOnlyList<PipelineStage> requisitionStages,
        IReadOnlyList<Interview> candidateInterviews)
    {
        var violations = new List<string>();

        foreach (var rule in _rules)
        {
            violations.AddRange(rule.Evaluate(candidate, targetStageId, requisitionStages, candidateInterviews));
        }

        return violations;
    }
}
