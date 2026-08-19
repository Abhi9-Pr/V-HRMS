using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Auth;

/// <summary>Always succeeds outwardly, whether or not the email matches a real account — telling
/// the caller which is true would let an attacker enumerate registered emails.</summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IReadRepository<User> _users;
    private readonly IUserCredentialStore _credentialStore;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ForgotPasswordCommandHandler(
        IReadRepository<User> users, IUserCredentialStore credentialStore, INotificationDispatcher notificationDispatcher)
    {
        _users = users;
        _credentialStore = credentialStore;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Success();
        }

        var user = await _users.FirstOrDefaultAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        var resetToken = await _credentialStore.GeneratePasswordResetTokenAsync(user.Id, cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage(
                user.Id.Value.ToString(),
                "Reset your Vespera password",
                $"Use this code to reset your password: {resetToken}",
                new Dictionary<string, string> { ["resetToken"] = resetToken }),
            cancellationToken);

        return Result.Success();
    }
}
