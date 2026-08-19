using Vespera.Domain.Common;

namespace Vespera.Domain.IdentityAccess;

public readonly record struct RoleId(Guid Value)
{
    public static RoleId New() => new(Guid.NewGuid());
}

public sealed class Role : AuditableTenantAggregateRoot<RoleId>
{
    private readonly List<PermissionId> _permissionIds = [];

    private Role(RoleId id, TenantId tenantId, string name, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
    }

    public string Name { get; private set; }

    public IReadOnlyCollection<PermissionId> PermissionIds => _permissionIds.AsReadOnly();

    public static Result<Role> Create(TenantId tenantId, string name, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Role>(Error.Validation("role.name_required", "Role name is required."));
        }

        return Result.Success(new Role(RoleId.New(), tenantId, name.Trim(), occurredOn, createdBy));
    }

    public Result Grant(PermissionId permissionId, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (_permissionIds.Contains(permissionId))
        {
            return Result.Failure(Error.Conflict("role.permission_already_granted", "Permission is already granted to this role."));
        }

        _permissionIds.Add(permissionId);
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Revoke(PermissionId permissionId, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (!_permissionIds.Remove(permissionId))
        {
            return Result.Failure(Error.NotFound("role.permission_not_granted", "Permission is not granted to this role."));
        }

        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
