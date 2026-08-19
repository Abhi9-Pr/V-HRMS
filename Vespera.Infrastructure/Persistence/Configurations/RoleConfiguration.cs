using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : TenantScopedEntityConfiguration<Role, RoleId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new RoleId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);

        // See UserConfiguration for why PermissionIds is mapped via its private backing field.
        builder.Ignore(e => e.PermissionIds);
        builder.Property<List<PermissionId>>("_permissionIds")
            .HasColumnName("PermissionIds")
            .HasConversion(
                permissionIds => JsonSerializer.Serialize(permissionIds.Select(p => p.Value).ToList(), (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!.Select(g => new PermissionId(g)).ToList())
            .Metadata.SetValueComparer(new ValueComparer<List<PermissionId>>(
                (left, right) => left!.SequenceEqual(right!),
                list => list.Aggregate(0, (hash, permissionId) => HashCode.Combine(hash, permissionId.Value)),
                list => list.ToList()));
    }
}
