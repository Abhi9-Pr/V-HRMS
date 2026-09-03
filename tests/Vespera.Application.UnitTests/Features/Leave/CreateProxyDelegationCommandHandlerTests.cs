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

public class CreateProxyDelegationCommandHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IWriteRepository<ProxyDelegation> _delegationWriter = Substitute.For<IWriteRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private CreateProxyDelegationCommandHandler CreateHandler() => new(_users, _delegationWriter, _tenantContext, _currentUser);

    private User CreateCallerUser(EmployeeId? employeeId)
    {
        var email = EmailAddress.Create("caller@vespera.test").Value;
        return User.Create(_tenantId, email, employeeId, Now, "system");
    }

    [Fact]
    public async Task Handle_Should_Create_A_ProxyDelegation_And_Return_Its_Id()
    {
        var userId = Guid.NewGuid();
        var delegatorEmployeeId = EmployeeId.New();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(CreateCallerUser(delegatorEmployeeId));

        var command = new CreateProxyDelegationCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), "LeaveApprovals");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _delegationWriter.Received(1).AddAsync(Arg.Any<ProxyDelegation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var command = new CreateProxyDelegationCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), "LeaveApprovals");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("proxy_delegation.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Caller_Has_No_Employee_Profile()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(CreateCallerUser(employeeId: null));

        var command = new CreateProxyDelegationCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), "LeaveApprovals");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("proxy_delegation.no_employee_profile");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Period_Is_Invalid()
    {
        var userId = Guid.NewGuid();
        var delegatorEmployeeId = EmployeeId.New();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(CreateCallerUser(delegatorEmployeeId));

        var command = new CreateProxyDelegationCommand(Guid.NewGuid(), new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 1), "LeaveApprovals");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Delegating_To_Self()
    {
        var userId = Guid.NewGuid();
        var delegatorEmployeeId = EmployeeId.New();
        _currentUser.UserId.Returns(userId);
        _tenantContext.TenantId.Returns(_tenantId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(CreateCallerUser(delegatorEmployeeId));

        var command = new CreateProxyDelegationCommand(delegatorEmployeeId.Value, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), "LeaveApprovals");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("proxy_delegation.self_delegation");
    }
}
