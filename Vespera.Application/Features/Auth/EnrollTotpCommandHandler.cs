using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed class EnrollTotpCommandHandler : IRequestHandler<EnrollTotpCommand, Result<EnrollTotpResult>>
{
    private readonly IUserCredentialStore _credentialStore;
    private readonly IOtpService _otpService;
    private readonly IReadRepository<User> _users;
    private readonly ICurrentUser _currentUser;

    public EnrollTotpCommandHandler(IUserCredentialStore credentialStore, IOtpService otpService, IReadRepository<User> users, ICurrentUser currentUser)
    {
        _credentialStore = credentialStore;
        _otpService = otpService;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<Result<EnrollTotpResult>> Handle(EnrollTotpCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<EnrollTotpResult>(Error.Unauthorized("auth.not_authenticated", "This action requires an authenticated user."));
        }

        var domainUserId = new UserId(userId);
        var user = await _users.FirstOrDefaultAsync(new UserByIdSpecification(domainUserId), cancellationToken);
        if (user is null)
        {
            return Result.Failure<EnrollTotpResult>(Error.NotFound("auth.user_not_found", "User not found."));
        }

        var secret = await _credentialStore.BeginTwoFactorEnrollmentAsync(domainUserId, cancellationToken);
        var qrCodeUri = _otpService.GenerateQrCodeUri(secret, user.Email.Value);

        return Result.Success(new EnrollTotpResult(secret, qrCodeUri));
    }
}
