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

public class GetMyDelegationsQueryHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<ProxyDelegation> _delegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private GetMyDelegationsQueryHandler CreateHandler() => new(_users, _delegations, _currentUser);

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    [Fact]
    public async Task Handle_Should_Return_The_Callers_Delegations()
    {
        var delegatorId = EmployeeId.New();
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(delegatorId));
        var delegation = ProxyDelegation.Create(
            _tenantId, delegatorId, EmployeeId.New(), DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10)).Value,
            DelegationScope.LeaveApprovals).Value;
        _delegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProxyDelegation> { delegation });

        var result = await CreateHandler().Handle(new GetMyDelegationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.Id == delegation.Id.Value && d.Scope == "LeaveApprovals" && !d.IsRevoked);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetMyDelegationsQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("proxy_delegation.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_Caller_Has_No_Employee_Profile()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(CreateCallerUser(null));

        var result = await CreateHandler().Handle(new GetMyDelegationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
