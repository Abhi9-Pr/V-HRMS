using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="QuarantinedBiometricPunch"/> is a plain <c>AggregateRoot&lt;TId&gt;</c> +
/// <c>ITenantScoped</c> (no soft-delete/audit stamps), same shape as
/// <see cref="RegularizationRequest"/> — configured directly rather than through either
/// tenant-scoped base class.
/// </summary>
public sealed class QuarantinedBiometricPunchConfiguration : IEntityTypeConfiguration<QuarantinedBiometricPunch>
{
    public void Configure(EntityTypeBuilder<QuarantinedBiometricPunch> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new QuarantinedBiometricPunchId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.BiometricDeviceId)
            .HasConversion(id => id.Value, value => new BiometricDeviceId(value))
            .IsRequired();

        builder.Property(e => e.DeviceUserId).IsRequired().HasMaxLength(128);
        builder.Property(e => e.PunchedAtUtc).IsRequired();
        builder.Property(e => e.PunchType).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.ExternalRecordId).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(e => e.ResolvedEmployeeId).HasConversion(
            id => id == null ? (Guid?)null : id.Value.Value,
            value => value == null ? (EmployeeId?)null : new EmployeeId(value.Value));

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        // Dedup key the poller checks before creating either a real punch or a fresh quarantine
        // row for the same device-reported record.
        builder.HasIndex(e => new { e.BiometricDeviceId, e.ExternalRecordId }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}
