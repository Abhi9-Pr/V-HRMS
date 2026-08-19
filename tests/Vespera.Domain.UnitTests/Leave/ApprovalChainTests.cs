using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Domain.UnitTests.Leave;

public class ApprovalChainTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

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

    private static ApprovalChain CreateChain(params EmployeeId[] approvers) =>
        ApprovalChain.Create(TenantId.New(), ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), approvers).Value;
}
