using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Leave.Events;

namespace Vespera.Domain.UnitTests.Leave;

public class ApprovalChainTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Raise_ApprovalStepAssigned_For_The_First_Step()
    {
        var firstApprover = EmployeeId.New();
        var chain = CreateChain(firstApprover);

        chain.DomainEvents.OfType<ApprovalStepAssigned>().Should().ContainSingle(
            assigned => assigned.StepIndex == 0 && assigned.ApproverId == firstApprover);
    }

    [Fact]
    public void Approve_Should_Advance_To_The_Next_Step_Without_Completing_The_Chain()
    {
        var firstApprover = EmployeeId.New();
        var secondApprover = EmployeeId.New();
        var chain = CreateChain(firstApprover, secondApprover);

        var result = chain.Approve(firstApprover, Now);

        result.IsSuccess.Should().BeTrue();
        chain.Status.Should().Be(ApprovalChainStatus.InProgress);
        chain.CurrentStepIndex.Should().Be(1);
        chain.DomainEvents.OfType<ApprovalStepAssigned>().Should().ContainSingle(assigned => assigned.StepIndex == 1);
    }

    [Fact]
    public void Approving_The_Final_Step_Should_Complete_The_Chain()
    {
        var firstApprover = EmployeeId.New();
        var secondApprover = EmployeeId.New();
        var chain = CreateChain(firstApprover, secondApprover);
        chain.Approve(firstApprover, Now);

        var result = chain.Approve(secondApprover, Now);

        result.IsSuccess.Should().BeTrue();
        chain.Status.Should().Be(ApprovalChainStatus.Approved);
        chain.DomainEvents.Should().ContainSingle(e => e is ApprovalChainApproved);
    }

    [Fact]
    public void Reject_At_Any_Step_Should_Reject_The_Whole_Chain()
    {
        var firstApprover = EmployeeId.New();
        var secondApprover = EmployeeId.New();
        var chain = CreateChain(firstApprover, secondApprover);

        var result = chain.Reject(firstApprover, Now, "Not justified");

        result.IsSuccess.Should().BeTrue();
        chain.Status.Should().Be(ApprovalChainStatus.Rejected);
        chain.DomainEvents.Should().ContainSingle(e => e is ApprovalChainRejected);
    }

    [Fact]
    public void Deciding_A_Chain_That_Is_No_Longer_InProgress_Should_Fail()
    {
        var approver = EmployeeId.New();
        var chain = CreateChain(approver);
        chain.Approve(approver, Now);

        var result = chain.Approve(approver, Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_Should_Succeed_While_InProgress()
    {
        var chain = CreateChain(EmployeeId.New());

        var result = chain.Cancel();

        result.IsSuccess.Should().BeTrue();
        chain.Status.Should().Be(ApprovalChainStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_Fail_Once_The_Chain_Has_Reached_A_Terminal_Status()
    {
        var approver = EmployeeId.New();
        var chain = CreateChain(approver);
        chain.Approve(approver, Now);

        var result = chain.Cancel();

        result.IsFailure.Should().BeTrue();
    }

    private static ApprovalChain CreateChain(params EmployeeId[] approvers) =>
        ApprovalChain.Create(TenantId.New(), ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), approvers, Now).Value;
}
