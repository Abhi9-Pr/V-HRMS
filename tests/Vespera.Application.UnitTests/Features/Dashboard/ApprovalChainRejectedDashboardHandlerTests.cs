using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Dashboard;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.Leave.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Dashboard;

public class ApprovalChainRejectedDashboardHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepositoryAdmin<ApprovalChain> _approvalChains = Substitute.For<IReadRepositoryAdmin<ApprovalChain>>();
    private readonly IReadRepositoryAdmin<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepositoryAdmin<ProxyDelegation>>();
    private readonly IReadRepositoryAdmin<User> _users = Substitute.For<IReadRepositoryAdmin<User>>();
    private readonly IDashboardRealtimePublisher _publisher = Substitute.For<IDashboardRealtimePublisher>();

    private ApprovalChainRejectedDashboardHandler CreateHandler() => new(_approvalChains, _proxyDelegations, _users, _publisher);

    [Fact]
    public async Task Handle_Should_Publish_The_Pending_Count_To_The_Decider()
    {
        var decider = EmployeeId.New();
        var user = User.Create(TenantId, EmailAddress.Create("decider@vespera.test").Value, decider, Now, "system");
        _approvalChains.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ApprovalChain>>(), Arg.Any<CancellationToken>()).Returns([]);
        _proxyDelegations.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);
        _users.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([user]);

        var notification = new DomainEventNotification<ApprovalChainRejected>(
            new ApprovalChainRejected(ApprovalChainId.New(), TenantId, ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), decider, "Not eligible", Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _publisher.Received(1).PublishApprovalsCountChangedAsync(
            user.Id.Value, Arg.Any<ApprovalsCountChangedPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Decider_Has_No_Linked_User()
    {
        _approvalChains.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ApprovalChain>>(), Arg.Any<CancellationToken>()).Returns([]);
        _proxyDelegations.ListIgnoringFiltersAsync(Arg.Any<ISpecification<ProxyDelegation>>(), Arg.Any<CancellationToken>()).Returns([]);
        _users.ListIgnoringFiltersAsync(Arg.Any<UserByEmployeeIdSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var notification = new DomainEventNotification<ApprovalChainRejected>(
            new ApprovalChainRejected(ApprovalChainId.New(), TenantId, ApprovalSubjectType.LeaveRequest, Guid.NewGuid(), EmployeeId.New(), "Not eligible", Now));

        await CreateHandler().Handle(notification, CancellationToken.None);

        await _publisher.DidNotReceive().PublishApprovalsCountChangedAsync(
            Arg.Any<Guid>(), Arg.Any<ApprovalsCountChangedPayload>(), Arg.Any<CancellationToken>());
    }
}
