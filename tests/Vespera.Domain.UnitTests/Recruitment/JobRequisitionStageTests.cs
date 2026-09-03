using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Domain.UnitTests.Recruitment;

public class JobRequisitionStageTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string ModifiedBy = "hr@vespera.test";

    private static JobRequisition CreateRequisition() =>
        JobRequisition.Create(TenantId.New(), "Senior Engineer", DepartmentId.New(), openingsCount: 2, Now, ModifiedBy).Value;

    [Fact]
    public void AddStage_Should_Append_A_PipelineStage_With_The_Next_SequenceNumber()
    {
        var requisition = CreateRequisition();

        requisition.AddStage("Screening", Now, ModifiedBy).IsSuccess.Should().BeTrue();
        requisition.AddStage("Offer", Now, ModifiedBy).IsSuccess.Should().BeTrue();

        requisition.Stages.Should().HaveCount(2);
        requisition.Stages[0].Name.Should().Be("Screening");
        requisition.Stages[0].SequenceNumber.Should().Be(0);
        requisition.Stages[1].Name.Should().Be("Offer");
        requisition.Stages[1].SequenceNumber.Should().Be(1);
    }

    [Fact]
    public void AddStage_Should_Fail_With_A_Blank_Name()
    {
        var requisition = CreateRequisition();

        var result = requisition.AddStage("   ", Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
        requisition.Stages.Should().BeEmpty();
    }

    [Fact]
    public void Close_Should_Set_Status_And_Fail_If_Already_Closed()
    {
        var requisition = CreateRequisition();

        requisition.Close(Now, ModifiedBy).IsSuccess.Should().BeTrue();
        requisition.Status.Should().Be(JobRequisitionStatus.Closed);

        requisition.Close(Now, ModifiedBy).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void PutOnHold_Then_Reopen_Should_Round_Trip_Status()
    {
        var requisition = CreateRequisition();

        requisition.PutOnHold(Now, ModifiedBy).IsSuccess.Should().BeTrue();
        requisition.Status.Should().Be(JobRequisitionStatus.OnHold);

        requisition.Reopen(Now, ModifiedBy).IsSuccess.Should().BeTrue();
        requisition.Status.Should().Be(JobRequisitionStatus.Open);
    }

    [Fact]
    public void PutOnHold_Should_Fail_When_Not_Open()
    {
        var requisition = CreateRequisition();
        requisition.Close(Now, ModifiedBy);

        var result = requisition.PutOnHold(Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reopen_Should_Fail_When_Not_OnHold()
    {
        var requisition = CreateRequisition();

        var result = requisition.Reopen(Now, ModifiedBy);

        result.IsFailure.Should().BeTrue();
    }
}
