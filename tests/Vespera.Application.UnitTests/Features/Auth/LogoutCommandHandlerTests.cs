using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Auth;

public class LogoutCommandHandlerTests
{
    private readonly IReadRepository<RefreshToken> _refreshTokens = Substitute.For<IReadRepository<RefreshToken>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private LogoutCommandHandler CreateHandler() => new(_refreshTokens, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Revoke_The_Active_Token_Belonging_To_The_Current_User()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _dateTimeProvider.UtcNow.Returns(_now);
        var token = RefreshToken.Issue(TenantId.New(), new UserId(userId), "hash", "device-1", _now, _now.AddDays(7)).Value;
        _refreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns(token);

        var result = await CreateHandler().Handle(new LogoutCommand("raw-refresh-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().Be(_now);
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_Token_Is_Unknown()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _refreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await CreateHandler().Handle(new LogoutCommand("unknown-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Succeed_Without_Revoking_When_Token_Belongs_To_Another_User()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(_now);
        var token = RefreshToken.Issue(TenantId.New(), UserId.New(), "hash", "device-1", _now, _now.AddDays(7)).Value;
        _refreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns(token);

        var result = await CreateHandler().Handle(new LogoutCommand("raw-refresh-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Succeed_Without_Reraising_When_Token_Already_Revoked()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _dateTimeProvider.UtcNow.Returns(_now);
        var token = RefreshToken.Issue(TenantId.New(), new UserId(userId), "hash", "device-1", _now, _now.AddDays(7)).Value;
        token.Revoke(_now);
        _refreshTokens.FirstOrDefaultAsync(Arg.Any<RefreshTokenByHashSpecification>(), Arg.Any<CancellationToken>()).Returns(token);

        var result = await CreateHandler().Handle(new LogoutCommand("raw-refresh-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
