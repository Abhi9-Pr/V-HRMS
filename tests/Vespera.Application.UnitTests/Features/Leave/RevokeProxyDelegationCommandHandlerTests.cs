using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class RevokeProxyDelegationCommandHandlerTests
{
    private readonly IReadRepository<ProxyDelegation> _delegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly IWriteRepository<ProxyDelegation> _delegationWriter = Substitute.For<IWriteRepository<ProxyDelegation>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private RevokeProxyDelegationCommandHandler CreateHandler() => new(_delegations, _delegationWriter, _users, _tenantContext, _currentUser);

    private static ProxyDelegation CreateDelegation(EmployeeId delegatorId) =>
        ProxyDelegation.Create(
            TenantId.New(), delegatorId, EmployeeId.New(),
            DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10)).Value, DelegationScope.LeaveApprovals).Value;

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    [Fact]
    public async Task Handle_Should_Revoke_The_Delegation()
    {
        var delegatorId = EmployeeId.New();
        var delegation = CreateDelegation(delegatorId);
        var userId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(_tenantId);
        _delegations.FirstOrDefaultAsync(Arg.Any<ProxyDelegationByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(delegation);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(delegatorId));

        var result = await CreateHandler().Handle(new RevokeProxyDelegationCommand(delegation.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delegation.IsRevoked.Should().BeTrue();
        _delegationWriter.Received(1).Update(delegation);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Delegation_Not_Found()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _delegations.FirstOrDefaultAsync(Arg.Any<ProxyDelegationByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((ProxyDelegation?)null);

        var result = await CreateHandler().Handle(new RevokeProxyDelegationCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("proxy_delegation.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        var delegation = CreateDelegation(EmployeeId.New());
        _tenantContext.TenantId.Returns(_tenantId);
        _delegations.FirstOrDefaultAsync(Arg.Any<ProxyDelegationByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(delegation);
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new RevokeProxyDelegationCommand(delegation.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("proxy_delegation.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Caller_Is_Not_The_Delegator()
    {
        var delegation = CreateDelegation(EmployeeId.New());
        var userId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(_tenantId);
        _delegations.FirstOrDefaultAsync(Arg.Any<ProxyDelegationByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(delegation);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(CreateCallerUser(EmployeeId.New()));

        var result = await CreateHandler().Handle(new RevokeProxyDelegationCommand(delegation.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("proxy_delegation.not_the_delegator");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Already_Revoked()
    {
        var delegatorId = EmployeeId.New();
        var delegation = CreateDelegation(delegatorId);
        delegation.Revoke();
        var userId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(_tenantId);
        _delegations.FirstOrDefaultAsync(Arg.Any<ProxyDelegationByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(delegation);
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(delegatorId));

        var result = await CreateHandler().Handle(new RevokeProxyDelegationCommand(delegation.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("proxy_delegation.already_revoked");
    }
}
