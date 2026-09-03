using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Dashboard.Widgets;

public class PendingApprovalsWidgetProviderTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IReadRepository<ApprovalChain> _approvalChains = Substitute.For<IReadRepository<ApprovalChain>>();
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public PendingApprovalsWidgetProviderTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private PendingApprovalsWidgetProvider CreateProvider() =>
        new(_approvalChains, _proxyDelegations, _tenantContext, _dateTimeProvider, new CurrentEmployeeResolver(_users, _currentUser));

    private void SignInAs(EmployeeId employeeId)
    {
        var userId = Guid.NewGuid();
        var user = User.Create(TenantId, EmailAddress.Create("approver@vespera.test").Value, employeeId, Now, "system");
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
    }

    private static ApprovalChain CreateChain(params EmployeeId[] approvers) =>
        ApprovalChain.Create(TenantId, ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), approvers, Now).Value;

    [Fact]
    public async Task GetPayloadAsync_Should_Return_Zero_When_There_Is_No_Signed_In_Employee()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().BeOfType<PendingApprovalsWidgetDto>().Subject;
        dto.TotalCount.Should().Be(0);
        dto.CountBySubjectType.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Count_Chains_Directly_Assigned_To_The_Employee_By_Subject_Type()
    {
        var approver = EmployeeId.New();
        SignInAs(approver);
        var chain = CreateChain(approver);
        _approvalChains.ListAsync(Arg.Any<InProgressApprovalChainsSpecification>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _proxyDelegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorsSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        var dto = result.Value.Should().BeOfType<PendingApprovalsWidgetDto>().Subject;
        dto.TotalCount.Should().Be(1);
        dto.CountBySubjectType.Should().ContainKey(nameof(ApprovalSubjectType.LeaveRequest)).WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Count_Chains_Delegated_To_The_Employee()
    {
        var nominalApprover = EmployeeId.New();
        var delegate_ = EmployeeId.New();
        SignInAs(delegate_);
        var chain = CreateChain(nominalApprover);
        var delegation = ProxyDelegation.Create(
            TenantId, nominalApprover, delegate_, DateRange.Create(Today.AddDays(-1), Today.AddDays(1)).Value, DelegationScope.All).Value;

        _approvalChains.ListAsync(Arg.Any<InProgressApprovalChainsSpecification>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _proxyDelegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorsSpecification>(), Arg.Any<CancellationToken>()).Returns([delegation]);

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        var dto = result.Value.Should().BeOfType<PendingApprovalsWidgetDto>().Subject;
        dto.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Not_Count_Chains_Assigned_To_A_Different_Approver()
    {
        var approver = EmployeeId.New();
        SignInAs(approver);
        var chain = CreateChain(EmployeeId.New());
        _approvalChains.ListAsync(Arg.Any<InProgressApprovalChainsSpecification>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _proxyDelegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorsSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        var dto = result.Value.Should().BeOfType<PendingApprovalsWidgetDto>().Subject;
        dto.TotalCount.Should().Be(0);
    }
}
