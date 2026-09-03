using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class LeaveApprovalStepAuthorizerTests
{
    private readonly IReadRepository<ProxyDelegation> _delegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private static readonly DateOnly Today = new(2026, 1, 5);

    private LeaveApprovalStepAuthorizer CreateAuthorizer() => new(_delegations);

    private static ProxyDelegation CreateDelegation(EmployeeId delegatorId, EmployeeId delegateId, DelegationScope scope, DateOnly from, DateOnly to) =>
        ProxyDelegation.Create(TenantId.New(), delegatorId, delegateId, DateRange.Create(from, to).Value, scope).Value;

    [Fact]
    public async Task ResolveAuthorizedApproverAsync_Should_Return_The_Nominal_Approver_When_No_Delegation_Exists()
    {
        var nominalApproverId = EmployeeId.New();
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProxyDelegation>());

        var result = await CreateAuthorizer().ResolveAuthorizedApproverAsync(nominalApproverId, Today, CancellationToken.None);

        result.Should().Be(nominalApproverId);
    }

    [Fact]
    public async Task ResolveAuthorizedApproverAsync_Should_Return_The_Delegate_When_An_Active_LeaveApprovals_Delegation_Exists()
    {
        var nominalApproverId = EmployeeId.New();
        var delegateId = EmployeeId.New();
        var delegation = CreateDelegation(
            nominalApproverId, delegateId, DelegationScope.LeaveApprovals, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProxyDelegation> { delegation });

        var result = await CreateAuthorizer().ResolveAuthorizedApproverAsync(nominalApproverId, Today, CancellationToken.None);

        result.Should().Be(delegateId);
    }

    [Fact]
    public async Task ResolveAuthorizedApproverAsync_Should_Return_The_Delegate_When_An_Active_All_Scope_Delegation_Exists()
    {
        var nominalApproverId = EmployeeId.New();
        var delegateId = EmployeeId.New();
        var delegation = CreateDelegation(
            nominalApproverId, delegateId, DelegationScope.All, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProxyDelegation> { delegation });

        var result = await CreateAuthorizer().ResolveAuthorizedApproverAsync(nominalApproverId, Today, CancellationToken.None);

        result.Should().Be(delegateId);
    }

    [Fact]
    public async Task ResolveAuthorizedApproverAsync_Should_Ignore_A_Delegation_With_A_NonMatching_Scope()
    {
        var nominalApproverId = EmployeeId.New();
        var delegateId = EmployeeId.New();
        var delegation = CreateDelegation(
            nominalApproverId, delegateId, DelegationScope.ExpenseApprovals, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProxyDelegation> { delegation });

        var result = await CreateAuthorizer().ResolveAuthorizedApproverAsync(nominalApproverId, Today, CancellationToken.None);

        result.Should().Be(nominalApproverId);
    }

    [Fact]
    public async Task ResolveAuthorizedApproverAsync_Should_Ignore_A_Delegation_Not_Active_Today()
    {
        var nominalApproverId = EmployeeId.New();
        var delegateId = EmployeeId.New();
        var delegation = CreateDelegation(
            nominalApproverId, delegateId, DelegationScope.LeaveApprovals, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 10));
        _delegations.ListAsync(Arg.Any<DelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProxyDelegation> { delegation });

        var result = await CreateAuthorizer().ResolveAuthorizedApproverAsync(nominalApproverId, Today, CancellationToken.None);

        result.Should().Be(nominalApproverId);
    }
}
