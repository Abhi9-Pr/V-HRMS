using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IReadRepository<RefreshToken> _refreshTokens;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutCommandHandler(
        IReadRepository<RefreshToken> refreshTokens, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = LoginCommandHandler.Hash(request.RefreshToken);
        var token = await _refreshTokens.FirstOrDefaultAsync(new RefreshTokenByHashSpecification(hash), cancellationToken);

        if (token is null || token.UserId.Value != _currentUser.UserId)
        {
            // Logout is idempotent from the caller's point of view — an unknown/foreign token
            // just means there's nothing to revoke, not an error.
            return Result.Success();
        }

        if (token.IsActive(_dateTimeProvider.UtcNow))
        {
            token.Revoke(_dateTimeProvider.UtcNow);
        }

        return Result.Success();
    }
}
