using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly IReadRepository<User> _readUsers = Substitute.For<IReadRepository<User>>();
    private readonly IWriteRepository<User> _writeUsers = Substitute.For<IWriteRepository<User>>();
    private readonly IUserCredentialStore _credentialStore = Substitute.For<IUserCredentialStore>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public RegisterUserCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(_now);
        _currentUser.UserId.Returns((Guid?)null);
        _readUsers.AnyAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns(false);
        _credentialStore.CreateAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
    }

    private RegisterUserCommandHandler CreateHandler() =>
        new(_readUsers, _writeUsers, _credentialStore, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Create_The_User_And_Return_Its_Id()
    {
        var command = new RegisterUserCommand("new.user@vespera.test", "super-secret-1", null, []);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _writeUsers.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Assign_The_Requested_Roles()
    {
        var roleId = Guid.NewGuid();
        var command = new RegisterUserCommand("new.user@vespera.test", "super-secret-1", null, [roleId]);
        User? createdUser = null;
        _writeUsers.AddAsync(Arg.Do<User>(u => createdUser = u), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        createdUser.Should().NotBeNull();
        createdUser!.RoleIds.Should().Contain(new RoleId(roleId));
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Email_Is_Malformed()
    {
        var command = new RegisterUserCommand("not-an-email", "super-secret-1", null, []);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _writeUsers.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Email_Is_Already_Registered()
    {
        _readUsers.AnyAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = new RegisterUserCommand("existing@vespera.test", "super-secret-1", null, []);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.email_already_registered");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Duplicate_RoleIds_Are_Requested()
    {
        var roleId = Guid.NewGuid();
        var command = new RegisterUserCommand("new.user@vespera.test", "super-secret-1", null, [roleId, roleId]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.role_already_assigned");
        await _writeUsers.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Credential_Creation_Fails()
    {
        _credentialStore.CreateAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Validation("auth.weak_password", "Password does not meet complexity requirements.")));
        var command = new RegisterUserCommand("new.user@vespera.test", "super-secret-1", null, []);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.weak_password");
        await _writeUsers.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
