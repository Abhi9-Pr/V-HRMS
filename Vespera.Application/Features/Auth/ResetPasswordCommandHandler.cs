using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Auth;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IReadRepository<User> _users;
    private readonly IUserCredentialStore _credentialStore;

    public ResetPasswordCommandHandler(IReadRepository<User> users, IUserCredentialStore credentialStore)
    {
        _users = users;
        _credentialStore = credentialStore;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure(Error.Unauthorized("auth.invalid_reset_token", "This reset token is not valid."));
        }

        var user = await _users.FirstOrDefaultAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.Unauthorized("auth.invalid_reset_token", "This reset token is not valid."));
        }

        return await _credentialStore.ResetPasswordAsync(user.Id, request.ResetToken, request.NewPassword, cancellationToken);
    }
}
