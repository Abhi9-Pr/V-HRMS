using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Infrastructure.Persistence.Configurations.Helpdesk;

public sealed class TicketCategoryConfiguration : TenantScopedEntityConfiguration<TicketCategory, TicketCategoryId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new TicketCategoryId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.DepartmentId).HasConversion(id => id.Value, value => new DepartmentId(value));
        builder.Property(e => e.DefaultSlaPolicyId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null, value => value.HasValue ? new SlaPolicyId(value.Value) : (SlaPolicyId?)null);
    }
}
