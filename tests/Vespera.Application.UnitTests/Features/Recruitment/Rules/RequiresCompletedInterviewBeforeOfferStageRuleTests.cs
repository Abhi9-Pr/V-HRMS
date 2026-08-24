using FluentAssertions;
using Vespera.Application.Features.Recruitment.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment.Rules;

public class RequiresCompletedInterviewBeforeOfferStageRuleTests
{
    private readonly TenantId _tenantId = TenantId.New();
    private readonly RequiresCompletedInterviewBeforeOfferStageRule _rule = new();

    [Fact]
    public void Evaluate_Should_Not_Violate_For_A_Non_Offer_Stage()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        requisition.AddStage("Screening", DateTimeOffset.UtcNow, "system");
        var candidate = CreateCandidate(requisition.Id);

        var violations = _rule.Evaluate(candidate, requisition.Stages[0].Id, requisition.Stages, []);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_Should_Violate_For_An_Offer_Stage_With_No_Completed_Interview()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        requisition.AddStage("Offer", DateTimeOffset.UtcNow, "system");
        var candidate = CreateCandidate(requisition.Id);

        var violations = _rule.Evaluate(candidate, requisition.Stages[0].Id, requisition.Stages, []);

        violations.Should().ContainSingle();
    }

    [Fact]
    public void Evaluate_Should_Not_Violate_For_An_Offer_Stage_With_A_Completed_Interview()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        requisition.AddStage("Offer", DateTimeOffset.UtcNow, "system");
        var candidate = CreateCandidate(requisition.Id);
        var interview = Interview.Schedule(
            _tenantId, candidate.Id, requisition.Stages[0].Id, DateTimeOffset.UtcNow.AddDays(-1), [EmployeeId.New()]).Value;
        interview.Complete("Great", 5);

        var violations = _rule.Evaluate(candidate, requisition.Stages[0].Id, requisition.Stages, [interview]);

        violations.Should().BeEmpty();
    }

    private Candidate CreateCandidate(JobRequisitionId requisitionId) =>
        Candidate.Create(
            _tenantId, requisitionId, "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
}
