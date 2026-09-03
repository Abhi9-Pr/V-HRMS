using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Dashboard;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Dashboard;

public class RealtimeApprovalsCounterTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IReadRepositoryAdmin<ApprovalChain> _approvalChains = Substitute.For<IReadRepositoryAdmin<ApprovalChain>>();
    private readonly IReadRepositoryAdmin<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepositoryAdmin<ProxyDelegation>>();

    private static ApprovalChain CreateChain(params EmployeeId[] approvers) =>
        ApprovalChain.Create(TenantId, ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), approvers, Now).Value;

    [Fact]
    public async Task Should_Count_A_Chain_Whose_Nominal_Approver_Matches_Directly()
    {
        var approver = EmployeeId.New();
        var chain = CreateChain(approver);
        _approvalChains.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ApprovalChain>>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _proxyDelegations.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);

        var count = await RealtimeApprovalsCounter.CountPendingForApproverAsync(
            _approvalChains, _proxyDelegations, TenantId, approver, Today, CancellationToken.None);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Should_Count_A_Chain_For_The_Active_Delegate_Instead_Of_The_Nominal_Approver()
    {
        var nominalApprover = EmployeeId.New();
        var delegate_ = EmployeeId.New();
        var chain = CreateChain(nominalApprover);
        var delegation = ProxyDelegation.Create(
            TenantId, nominalApprover, delegate_, DateRange.Create(Today.AddDays(-1), Today.AddDays(1)).Value, DelegationScope.All).Value;

        _approvalChains.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ApprovalChain>>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _proxyDelegations.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([delegation]);

        var forNominal = await RealtimeApprovalsCounter.CountPendingForApproverAsync(
            _approvalChains, _proxyDelegations, TenantId, nominalApprover, Today, CancellationToken.None);
        var forDelegate = await RealtimeApprovalsCounter.CountPendingForApproverAsync(
            _approvalChains, _proxyDelegations, TenantId, delegate_, Today, CancellationToken.None);

        forNominal.Should().Be(0);
        forDelegate.Should().Be(1);
    }

    [Fact]
    public async Task Should_Return_Zero_When_No_Chain_Resolves_To_The_Given_Approver()
    {
        var chain = CreateChain(EmployeeId.New());
        _approvalChains.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ApprovalChain>>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _proxyDelegations.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);

        var count = await RealtimeApprovalsCounter.CountPendingForApproverAsync(
            _approvalChains, _proxyDelegations, TenantId, EmployeeId.New(), Today, CancellationToken.None);

        count.Should().Be(0);
    }
}
