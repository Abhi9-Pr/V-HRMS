using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class EventRsvpConfiguration : IEntityTypeConfiguration<EventRsvp>
{
    public void Configure(EntityTypeBuilder<EventRsvp> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EventRsvpId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.CorporateEventId).HasConversion(id => id.Value, value => new CorporateEventId(value)).IsRequired();
        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
        builder.Property(e => e.Response).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.RespondedAt);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.CorporateEventId, e.EmployeeId }).IsUnique();
    }
}
