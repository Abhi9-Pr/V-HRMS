using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Domain.UnitTests.Recruitment;

public class JobRequisitionApprovalTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string ModifiedBy = "hr@vespera.test";

    [Fact]
    public void Full_Approval_And_Publish_Happy_Path()
    {
        var requisition = CreateRequisition();

        requisition.SubmitForApproval(Now, ModifiedBy).IsSuccess.Should().BeTrue();
        requisition.ApprovalStatus.Should().Be(RequisitionApprovalStatus.PendingApproval);

        requisition.ApproveRequisition(Now, ModifiedBy).IsSuccess.Should().BeTrue();
        requisition.ApprovalStatus.Should().Be(RequisitionApprovalStatus.Approved);

        requisition.Publish(Now, ModifiedBy).IsSuccess.Should().BeTrue();
        requisition.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void SubmitForApproval_Should_Fail_When_Not_Draft()
    {
        var requisition = CreateRequisition();
        requisition.SubmitForApproval(Now, ModifiedBy);

        var result = requisition.SubmitForApproval(Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ApproveRequisition_Should_Fail_Before_Submission()
    {
        var requisition = CreateRequisition();

        var result = requisition.ApproveRequisition(Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RejectRequisition_Should_Set_Reason_And_Status()
    {
        var requisition = CreateRequisition();
        requisition.SubmitForApproval(Now, ModifiedBy);

        var result = requisition.RejectRequisition("Headcount frozen", Now, ModifiedBy);

        result.IsSuccess.Should().BeTrue();
        requisition.ApprovalStatus.Should().Be(RequisitionApprovalStatus.Rejected);
        requisition.RejectionReason.Should().Be("Headcount frozen");
    }

    [Fact]
    public void RejectRequisition_Should_Fail_With_Blank_Reason()
    {
        var requisition = CreateRequisition();
        requisition.SubmitForApproval(Now, ModifiedBy);

        var result = requisition.RejectRequisition("   ", Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Publish_Should_Fail_Before_Approval()
    {
        var requisition = CreateRequisition();

        var result = requisition.Publish(Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Publish_Should_Fail_While_OnHold()
    {
        var requisition = CreateRequisition();
        requisition.SubmitForApproval(Now, ModifiedBy);
        requisition.ApproveRequisition(Now, ModifiedBy);
        requisition.PutOnHold(Now, ModifiedBy);

        var result = requisition.Publish(Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Publish_Should_Fail_When_Already_Published()
    {
        var requisition = CreateRequisition();
        requisition.SubmitForApproval(Now, ModifiedBy);
        requisition.ApproveRequisition(Now, ModifiedBy);
        requisition.Publish(Now, ModifiedBy);

        var result = requisition.Publish(Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
    }

    private static JobRequisition CreateRequisition() =>
        JobRequisition.Create(TenantId.New(), "Senior Engineer", DepartmentId.New(), openingsCount: 2, Now, ModifiedBy).Value;
}
