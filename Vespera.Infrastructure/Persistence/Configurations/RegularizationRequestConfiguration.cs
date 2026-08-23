using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="RegularizationRequest"/> is a plain <c>AggregateRoot&lt;TId&gt;</c> +
/// <c>ITenantScoped</c> (no soft-delete/audit stamps), same shape as <see cref="ShiftRoster"/> —
/// configured directly rather than through either tenant-scoped base class.
/// </summary>
public sealed class RegularizationRequestConfiguration : IEntityTypeConfiguration<RegularizationRequest>
{
    public void Configure(EntityTypeBuilder<RegularizationRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new RegularizationRequestId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new Vespera.Domain.Common.TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.AttendanceDayId).HasConversion(id => id.Value, value => new AttendanceDayId(value));

        builder.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.EvidenceFileReference).HasMaxLength(512);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.ApproverId).HasConversion(
            id => id == null ? (Guid?)null : id.Value.Value,
            value => value == null ? (EmployeeId?)null : new EmployeeId(value.Value));
        builder.Property(e => e.RejectionReason).HasMaxLength(1000);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}
