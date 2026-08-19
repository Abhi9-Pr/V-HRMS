using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Auth;

/// <summary>Admin-only — enforced by <c>[HasPermission(Permissions.Users.Manage)]</c> on the
/// controller action, not here; a command has no notion of HTTP authorization.</summary>
public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IReadRepository<User> _readUsers;
    private readonly IWriteRepository<User> _writeUsers;
    private readonly IUserCredentialStore _credentialStore;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterUserCommandHandler(
        IReadRepository<User> readUsers, IWriteRepository<User> writeUsers, IUserCredentialStore credentialStore,
        ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _readUsers = readUsers;
        _writeUsers = writeUsers;
        _credentialStore = credentialStore;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        if (await _readUsers.AnyAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("auth.email_already_registered", "A user with this email already exists."));
        }

        var now = _dateTimeProvider.UtcNow;
        var createdBy = _currentUser.UserId?.ToString() ?? "system";
        var employeeId = request.EmployeeId is { } id ? new EmployeeId(id) : (EmployeeId?)null;

        var user = User.Create(_tenantContext.TenantId, emailResult.Value, employeeId, now, createdBy);

        foreach (var roleId in request.RoleIds)
        {
            var assignResult = user.AssignRole(new RoleId(roleId), now, createdBy);
            if (assignResult.IsFailure)
            {
                return Result.Failure<Guid>(assignResult.Error);
            }
        }

        var credentialResult = await _credentialStore.CreateAsync(user.Id, emailResult.Value.Value, request.Password, cancellationToken);
        if (credentialResult.IsFailure)
        {
            return Result.Failure<Guid>(credentialResult.Error);
        }

        await _writeUsers.AddAsync(user, cancellationToken);

        return Result.Success(user.Id.Value);
    }
}
