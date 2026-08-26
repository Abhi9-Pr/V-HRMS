using FluentAssertions;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Assets;

public class AssetAssignmentConditionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CaptureHandoverSignature_Should_Fail_When_Reference_Is_Blank()
    {
        var assignment = AssetAssignment.Assign(TenantId.New(), AssetId.New(), EmployeeId.New(), Now);

        var result = assignment.CaptureHandoverSignature(" ");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CaptureHandoverSignature_Should_Set_The_Reference()
    {
        var assignment = AssetAssignment.Assign(TenantId.New(), AssetId.New(), EmployeeId.New(), Now);

        var result = assignment.CaptureHandoverSignature("signatures/handover-1.png");

        result.IsSuccess.Should().BeTrue();
        assignment.HandoverSignatureReference.Should().Be("signatures/handover-1.png");
    }

    [Fact]
    public void RecordConditionReport_Should_Accumulate_Multiple_Reports()
    {
        var assignment = AssetAssignment.Assign(TenantId.New(), AssetId.New(), EmployeeId.New(), Now);

        assignment.RecordConditionReport(AssetConditionRating.Excellent, "As new at handover", Now, "admin@vespera.test");
        assignment.RecordConditionReport(AssetConditionRating.Good, "Minor scuff", Now.AddDays(90), "admin@vespera.test");

        assignment.ConditionReports.Should().HaveCount(2);
        assignment.ConditionReports[0].Rating.Should().Be(AssetConditionRating.Excellent);
        assignment.ConditionReports[1].Rating.Should().Be(AssetConditionRating.Good);
    }
}
