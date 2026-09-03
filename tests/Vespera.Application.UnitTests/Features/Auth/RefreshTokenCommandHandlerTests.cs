using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly IReadRepository<RefreshToken> _readRefreshTokens = Substitute.For<IReadRepository<RefreshToken>>();
    private readonly IWriteRepository<RefreshToken> _writeRefreshTokens = Substitute.For<IWriteRepository<RefreshToken>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IReadRepository<Role> _roles = Substitute.For<IReadRepository<Role>>();
    private readonly IReadRepository<Permission> _permissions = Substitute.For<IReadRepository<Permission>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public RefreshTokenCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(_now);
        _roles.ListAsync(Arg.Any<RolesByIdsSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private RefreshTokenCommandHandler CreateHandler() => new(
        _readRefreshTokens, _writeRefreshTokens, _users, _tokenService, _tenantContext, _dateTimeProvider,
        new PermissionResolver(_roles, _permissions), _unitOfWork);

    [Fact]
    public async Task Handle_Should_Fail_When_Token_Is_Unknown()
    {
        _readRefreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("unknown-token", "device-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_refresh_token");
    }

    [Fact]
    public async Task Handle_Should_Revoke_The_Family_And_Fail_When_Token_Reuse_Is_Detected()
    {
        var userId = UserId.New();
        var original = RefreshToken.Issue(_tenantId, userId, "hash-1", "device-1", _now, _now.AddDays(7)).Value;
        var rotated = original.Rotate("hash-2", _now, _now.AddDays(7)).Value;
        _readRefreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns(original);
        _readRefreshTokens.ListAsync(Arg.Any<ActiveRefreshTokensByFamilySpecification>(), Arg.Any<CancellationToken>()).Returns([rotated]);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("stolen-token", "device-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.refresh_token_reused");
        rotated.RevokedAt.Should().Be(_now);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Token_Is_Inactive()
    {
        var token = RefreshToken.Issue(_tenantId, UserId.New(), "hash", "device-1", _now.AddDays(-10), _now.AddDays(-3)).Value;
        _readRefreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns(token);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("expired-token", "device-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_refresh_token");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_User_Is_Not_Found()
    {
        var token = RefreshToken.Issue(_tenantId, UserId.New(), "hash", "device-1", _now, _now.AddDays(7)).Value;
        _readRefreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns(token);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("valid-token", "device-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_refresh_token");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_User_Is_Not_Active()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        var user = User.Create(_tenantId, email, employeeId: null, _now, "system");
        user.Lock(_now, "system");
        var token = RefreshToken.Issue(_tenantId, user.Id, "hash", "device-1", _now, _now.AddDays(7)).Value;
        _readRefreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns(token);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("valid-token", "device-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_refresh_token");
    }

    [Fact]
    public async Task Handle_Should_Rotate_The_Token_And_Return_New_Tokens_On_Success()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        var user = User.Create(_tenantId, email, employeeId: null, _now, "system");
        var token = RefreshToken.Issue(_tenantId, user.Id, "hash", "device-1", _now, _now.AddDays(7)).Value;
        _readRefreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns(token);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
        _tokenService.GenerateRefreshToken().Returns("next-raw-token");
        _tokenService.GenerateAccessToken(Arg.Any<TokenClaims>()).Returns("access-token");

        var result = await CreateHandler().Handle(new RefreshTokenCommand("valid-token", "device-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("next-raw-token");
        token.RevokedAt.Should().Be(_now);
        await _writeRefreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }
}
