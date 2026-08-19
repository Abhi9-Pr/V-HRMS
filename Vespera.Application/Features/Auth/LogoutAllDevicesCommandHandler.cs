using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed class LogoutAllDevicesCommandHandler : IRequestHandler<LogoutAllDevicesCommand, Result>
{
    private readonly IReadRepository<RefreshToken> _refreshTokens;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutAllDevicesCommandHandler(IReadRepository<RefreshToken> refreshTokens, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(LogoutAllDevicesCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(Error.Unauthorized("auth.not_authenticated", "This action requires an authenticated user."));
        }

        var tokens = await _refreshTokens.ListAsync(new ActiveRefreshTokensByUserSpecification(new UserId(userId)), cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        foreach (var token in tokens)
        {
            token.Revoke(now);
        }

        return Result.Success();
    }
}
