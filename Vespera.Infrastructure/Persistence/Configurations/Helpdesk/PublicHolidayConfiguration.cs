using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Infrastructure.Persistence.Configurations.Helpdesk;

/// <summary>PublicHoliday is AggregateRoot+ITenantScoped, not AuditableTenantAggregateRoot, so
/// it's configured directly — mirrors ExpenseClaimConfiguration's shape.</summary>
public sealed class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new PublicHolidayId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.Date });

        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
    }
}
