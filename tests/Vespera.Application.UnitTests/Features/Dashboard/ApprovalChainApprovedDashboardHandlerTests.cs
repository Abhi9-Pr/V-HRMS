using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Dashboard;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Leave.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Dashboard;

public class ApprovalChainApprovedDashboardHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepositoryAdmin<ApprovalChain> _approvalChains = Substitute.For<IReadRepositoryAdmin<ApprovalChain>>();
    private readonly IReadRepositoryAdmin<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepositoryAdmin<ProxyDelegation>>();
    private readonly IReadRepositoryAdmin<User> _users = Substitute.For<IReadRepositoryAdmin<User>>();
    private readonly IDashboardRealtimePublisher _publisher = Substitute.For<IDashboardRealtimePublisher>();

    private ApprovalChainApprovedDashboardHandler CreateHandler() => new(_approvalChains, _proxyDelegations, _users, _publisher);

    [Fact]
    public async Task Handle_Should_Publish_The_Pending_Count_To_The_Final_Approvers_User()
    {
        var finalApprover = EmployeeId.New();
        var chain = ApprovalChain.Create(TenantId, ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), [finalApprover], Now).Value;
        chain.Approve(finalApprover, Now);
        var user = User.Create(TenantId, EmailAddress.Create("final@vespera.test").Value, finalApprover, Now, "system");

        _approvalChains.ListIgnoringFiltersAsync(Arg.Any<ApprovalChainByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _approvalChains.ListIgnoringFiltersAsync(Arg.Any<InProgressApprovalChainsSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
        _proxyDelegations.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);
        _users.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([user]);

        var notification = new DomainEventNotification<ApprovalChainApproved>(
            new ApprovalChainApproved(chain.Id, TenantId, ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _publisher.Received(1).PublishApprovalsCountChangedAsync(
            user.Id.Value, Arg.Any<ApprovalsCountChangedPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Chain_Cannot_Be_Found()
    {
        _approvalChains.ListIgnoringFiltersAsync(Arg.Any<ApprovalChainByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var notification = new DomainEventNotification<ApprovalChainApproved>(
            new ApprovalChainApproved(ApprovalChainId.New(), TenantId, ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _publisher.DidNotReceive().PublishApprovalsCountChangedAsync(
            Arg.Any<Guid>(), Arg.Any<ApprovalsCountChangedPayload>(), Arg.Any<CancellationToken>());
    }
}
