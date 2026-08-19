using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : TenantScopedEntityConfiguration<User, UserId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new UserId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Email)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value).Value)
            .IsRequired()
            .HasMaxLength(254);
        builder.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();

        builder.Property(e => e.EmployeeId)
            .HasConversion(id => id != null ? id.Value.Value : (Guid?)null, value => value != null ? new EmployeeId(value.Value) : (EmployeeId?)null);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        // RoleIds is a read-only projection (IReadOnlyCollection<RoleId>) over the private
        // `_roleIds` List<RoleId> field; the field is what actually persists, as a JSON array of
        // guids, with an explicit value comparer so EF detects in-place Add/Remove mutations.
        builder.Ignore(e => e.RoleIds);
        builder.Property<List<RoleId>>("_roleIds")
            .HasColumnName("RoleIds")
            .HasConversion(
                roleIds => JsonSerializer.Serialize(roleIds.Select(r => r.Value).ToList(), (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)!.Select(g => new RoleId(g)).ToList())
            .Metadata.SetValueComparer(new ValueComparer<List<RoleId>>(
                (left, right) => left!.SequenceEqual(right!),
                list => list.Aggregate(0, (hash, roleId) => HashCode.Combine(hash, roleId.Value)),
                list => list.ToList()));
    }
}
