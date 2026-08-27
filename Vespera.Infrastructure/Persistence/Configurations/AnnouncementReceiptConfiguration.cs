using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class AnnouncementReceiptConfiguration : IEntityTypeConfiguration<AnnouncementReceipt>
{
    public void Configure(EntityTypeBuilder<AnnouncementReceipt> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new AnnouncementReceiptId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.AnnouncementId).HasConversion(id => id.Value, value => new AnnouncementId(value)).IsRequired();
        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
        builder.Property(e => e.AcknowledgedAt);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.AnnouncementId, e.EmployeeId }).IsUnique();
    }
}
