using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IWriteRepository<RefreshToken> _refreshTokens = Substitute.For<IWriteRepository<RefreshToken>>();
    private readonly IUserCredentialStore _credentialStore = Substitute.For<IUserCredentialStore>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IReadRepository<Role> _roles = Substitute.For<IReadRepository<Role>>();
    private readonly IReadRepository<Permission> _permissions = Substitute.For<IReadRepository<Permission>>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public LoginCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(_now);
        _roles.ListAsync(Arg.Any<RolesByIdsSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private LoginCommandHandler CreateHandler() =>
        new(_users, _refreshTokens, _credentialStore, _tokenService, _tenantContext, _dateTimeProvider, new PermissionResolver(_roles, _permissions));

    private User CreateActiveUser(EmailAddress email)
    {
        var user = User.Create(_tenantId, email, employeeId: null, _now, "system");
        _users.FirstOrDefaultAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task Handle_Should_Issue_Tokens_On_Valid_Credentials()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        var user = CreateActiveUser(email);
        _credentialStore.VerifyPasswordAsync(user.Id, "password", Arg.Any<CancellationToken>()).Returns(true);
        _tokenService.GenerateAccessToken(Arg.Any<TokenClaims>()).Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");

        var result = await CreateHandler().Handle(new LoginCommand("user@vespera.test", "password", "device-1", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.AccessTokenExpiresAt.Should().Be(_now.Add(AuthTokenLifetimes.AccessToken));
        await _refreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Email_Is_Malformed()
    {
        var result = await CreateHandler().Handle(new LoginCommand("not-an-email", "password", "device-1", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_credentials");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_User_Does_Not_Exist()
    {
        _users.FirstOrDefaultAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new LoginCommand("nobody@vespera.test", "password", "device-1", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_credentials");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Password_Is_Wrong()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        var user = CreateActiveUser(email);
        _credentialStore.VerifyPasswordAsync(user.Id, "wrong-password", Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(new LoginCommand("user@vespera.test", "wrong-password", "device-1", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_credentials");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Account_Is_Not_Active()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        var user = CreateActiveUser(email);
        user.Lock(_now, "system");
        _credentialStore.VerifyPasswordAsync(user.Id, "password", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(new LoginCommand("user@vespera.test", "password", "device-1", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.account_not_active");
    }

    [Fact]
    public async Task Handle_Should_Require_Totp_Enrollment_For_Finance_Admin_Without_TwoFactor()
    {
        var email = EmailAddress.Create("finance@vespera.test").Value;
        var user = CreateActiveUser(email);
        var role = Role.Create(_tenantId, "Finance", _now, "system").Value;
        var permission = Permission.Create(Permissions.Finance.Admin, "Finance admin").Value;
        role.Grant(permission.Id, _now, "system");
        user.AssignRole(role.Id, _now, "system");
        _roles.ListAsync(Arg.Any<RolesByIdsSpecification>(), Arg.Any<CancellationToken>()).Returns([role]);
        _permissions.ListAsync(Arg.Any<PermissionsByIdsSpecification>(), Arg.Any<CancellationToken>()).Returns([permission]);
        _credentialStore.VerifyPasswordAsync(user.Id, "password", Arg.Any<CancellationToken>()).Returns(true);
        _credentialStore.IsTwoFactorEnabledAsync(user.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(new LoginCommand("finance@vespera.test", "password", "device-1", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.totp_enrollment_required");
    }

    [Fact]
    public async Task Handle_Should_Require_Totp_Code_For_Finance_Admin_With_TwoFactor_Enabled()
    {
        var email = EmailAddress.Create("finance@vespera.test").Value;
        var user = CreateActiveUser(email);
        var role = Role.Create(_tenantId, "Finance", _now, "system").Value;
        var permission = Permission.Create(Permissions.Finance.Admin, "Finance admin").Value;
        role.Grant(permission.Id, _now, "system");
        user.AssignRole(role.Id, _now, "system");
        _roles.ListAsync(Arg.Any<RolesByIdsSpecification>(), Arg.Any<CancellationToken>()).Returns([role]);
        _permissions.ListAsync(Arg.Any<PermissionsByIdsSpecification>(), Arg.Any<CancellationToken>()).Returns([permission]);
        _credentialStore.VerifyPasswordAsync(user.Id, "password", Arg.Any<CancellationToken>()).Returns(true);
        _credentialStore.IsTwoFactorEnabledAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(new LoginCommand("finance@vespera.test", "password", "device-1", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.totp_required");
    }

    [Fact]
    public async Task Handle_Should_Succeed_For_Finance_Admin_With_Valid_Totp_Code()
    {
        var email = EmailAddress.Create("finance@vespera.test").Value;
        var user = CreateActiveUser(email);
        var role = Role.Create(_tenantId, "Finance", _now, "system").Value;
        var permission = Permission.Create(Permissions.Finance.Admin, "Finance admin").Value;
        role.Grant(permission.Id, _now, "system");
        user.AssignRole(role.Id, _now, "system");
        _roles.ListAsync(Arg.Any<RolesByIdsSpecification>(), Arg.Any<CancellationToken>()).Returns([role]);
        _permissions.ListAsync(Arg.Any<PermissionsByIdsSpecification>(), Arg.Any<CancellationToken>()).Returns([permission]);
        _credentialStore.VerifyPasswordAsync(user.Id, "password", Arg.Any<CancellationToken>()).Returns(true);
        _credentialStore.IsTwoFactorEnabledAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _credentialStore.ValidateTwoFactorCodeAsync(user.Id, "123456", Arg.Any<CancellationToken>()).Returns(true);
        _tokenService.GenerateAccessToken(Arg.Any<TokenClaims>()).Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");

        var result = await CreateHandler().Handle(new LoginCommand("finance@vespera.test", "password", "device-1", "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
