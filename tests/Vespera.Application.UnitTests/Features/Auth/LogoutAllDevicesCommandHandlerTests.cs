using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Auth;

public class LogoutAllDevicesCommandHandlerTests
{
    private readonly IReadRepository<RefreshToken> _refreshTokens = Substitute.For<IReadRepository<RefreshToken>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private LogoutAllDevicesCommandHandler CreateHandler() => new(_refreshTokens, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Revoke_All_Active_Tokens_For_The_User()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _dateTimeProvider.UtcNow.Returns(_now);
        var tokenA = RefreshToken.Issue(TenantId.New(), new UserId(userId), "hash-a", "device-a", _now, _now.AddDays(7)).Value;
        var tokenB = RefreshToken.Issue(TenantId.New(), new UserId(userId), "hash-b", "device-b", _now, _now.AddDays(7)).Value;
        _refreshTokens.ListAsync(Arg.Any<ActiveRefreshTokensByUserSpecification>(), Arg.Any<CancellationToken>()).Returns([tokenA, tokenB]);

        var result = await CreateHandler().Handle(new LogoutAllDevicesCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tokenA.RevokedAt.Should().Be(_now);
        tokenB.RevokedAt.Should().Be(_now);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new LogoutAllDevicesCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.not_authenticated");
    }
}
