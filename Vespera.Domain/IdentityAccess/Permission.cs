using Vespera.Domain.Common;

namespace Vespera.Domain.IdentityAccess;

public readonly record struct PermissionId(Guid Value)
{
    public static PermissionId New() => new(Guid.NewGuid());
}

/// <summary>A system-wide permission catalog entry. Not tenant-scoped — the catalog itself is global.</summary>
public sealed class Permission : Entity<PermissionId>
{
    private Permission(PermissionId id, string code, string description)
        : base(id)
    {
        Code = code;
        Description = description;
    }

    public string Code { get; }

    public string Description { get; private set; }

    public static Result<Permission> Create(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Permission>(Error.Validation("permission.code_required", "Permission code is required."));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure<Permission>(Error.Validation("permission.description_required", "Permission description is required."));
        }

        return Result.Success(new Permission(PermissionId.New(), code.Trim(), description.Trim()));
    }

    public Result UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure(Error.Validation("permission.description_required", "Permission description is required."));
        }

        Description = description.Trim();
        return Result.Success();
    }
}
