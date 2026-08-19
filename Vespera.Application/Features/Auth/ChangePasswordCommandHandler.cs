using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUserCredentialStore _credentialStore;
    private readonly ICurrentUser _currentUser;

    public ChangePasswordCommandHandler(IUserCredentialStore credentialStore, ICurrentUser currentUser)
    {
        _credentialStore = credentialStore;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(Error.Unauthorized("auth.not_authenticated", "This action requires an authenticated user."));
        }

        return await _credentialStore.ChangePasswordAsync(new UserId(userId), request.CurrentPassword, request.NewPassword, cancellationToken);
    }
}
