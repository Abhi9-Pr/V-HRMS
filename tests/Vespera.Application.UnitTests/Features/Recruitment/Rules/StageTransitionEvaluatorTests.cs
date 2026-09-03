using FluentAssertions;
using NSubstitute;
using Vespera.Application.Features.Recruitment.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment.Rules;

public class StageTransitionEvaluatorTests
{
    private static readonly TenantId TenantId = TenantId.New();

    private static Candidate CreateCandidate()
    {
        var requisition = JobRequisition.Create(TenantId, "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        return Candidate.Create(
            TenantId, requisition.Id, "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
    }

    [Fact]
    public void Evaluate_Should_Combine_Violations_From_Every_Rule()
    {
        var ruleA = Substitute.For<IStageTransitionRule>();
        ruleA.Order.Returns(1);
        ruleA.Evaluate(Arg.Any<Candidate>(), Arg.Any<PipelineStageId>(), Arg.Any<IReadOnlyList<PipelineStage>>(), Arg.Any<IReadOnlyList<Interview>>())
            .Returns(["violation from rule A"]);

        var ruleB = Substitute.For<IStageTransitionRule>();
        ruleB.Order.Returns(2);
        ruleB.Evaluate(Arg.Any<Candidate>(), Arg.Any<PipelineStageId>(), Arg.Any<IReadOnlyList<PipelineStage>>(), Arg.Any<IReadOnlyList<Interview>>())
            .Returns(["violation from rule B"]);

        var evaluator = new StageTransitionEvaluator([ruleB, ruleA]);

        var violations = evaluator.Evaluate(CreateCandidate(), PipelineStageId.New(), [], []);

        violations.Should().Equal("violation from rule A", "violation from rule B");
    }

    [Fact]
    public void Evaluate_Should_Return_No_Violations_When_Every_Rule_Passes()
    {
        var rule = Substitute.For<IStageTransitionRule>();
        rule.Order.Returns(1);
        rule.Evaluate(Arg.Any<Candidate>(), Arg.Any<PipelineStageId>(), Arg.Any<IReadOnlyList<PipelineStage>>(), Arg.Any<IReadOnlyList<Interview>>())
            .Returns([]);

        var evaluator = new StageTransitionEvaluator([rule]);

        var violations = evaluator.Evaluate(CreateCandidate(), PipelineStageId.New(), [], []);

        violations.Should().BeEmpty();
    }
}
