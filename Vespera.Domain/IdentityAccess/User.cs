using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.IdentityAccess;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
}

public enum UserStatus
{
    Active,
    Locked,
    Deactivated,
}

/// <summary>
/// The domain-side identity record: who this user is and what they're allowed to do.
/// Credentials (password hashes, external logins) are an Infrastructure/ASP.NET Identity
/// concern, deliberately not modeled here.
/// </summary>
public sealed class User : AuditableTenantAggregateRoot<UserId>
{
    private readonly List<RoleId> _roleIds = [];

    private User(UserId id, TenantId tenantId, EmailAddress email, EmployeeId? employeeId, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Email = email;
        EmployeeId = employeeId;
        Status = UserStatus.Active;
    }

    public EmailAddress Email { get; private set; }

    public EmployeeId? EmployeeId { get; }

    public UserStatus Status { get; private set; }

    public IReadOnlyCollection<RoleId> RoleIds => _roleIds.AsReadOnly();

    public static User Create(TenantId tenantId, EmailAddress email, EmployeeId? employeeId, DateTimeOffset occurredOn, string createdBy) =>
        new(UserId.New(), tenantId, email, employeeId, occurredOn, createdBy);

    public Result AssignRole(RoleId roleId, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (_roleIds.Contains(roleId))
        {
            return Result.Failure(Error.Conflict("user.role_already_assigned", "Role is already assigned to this user."));
        }

        _roleIds.Add(roleId);
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result RevokeRole(RoleId roleId, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (!_roleIds.Remove(roleId))
        {
            return Result.Failure(Error.NotFound("user.role_not_assigned", "Role is not assigned to this user."));
        }

        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Lock(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == UserStatus.Locked)
        {
            return Result.Failure(Error.Conflict("user.already_locked", "User is already locked."));
        }

        Status = UserStatus.Locked;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Reactivate(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == UserStatus.Active)
        {
            return Result.Failure(Error.Conflict("user.already_active", "User is already active."));
        }

        Status = UserStatus.Active;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    /// <summary>Permanently ends this login (as opposed to <see cref="Lock"/>, which is a
    /// reversible security hold) — what an offboarding sweep calls once an employee's last
    /// working day has passed.</summary>
    public Result Deactivate(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == UserStatus.Deactivated)
        {
            return Result.Failure(Error.Conflict("user.already_deactivated", "User is already deactivated."));
        }

        Status = UserStatus.Deactivated;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
