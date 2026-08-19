using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class ReportingRelationshipConfiguration : TenantScopedReferenceEntityConfiguration<ReportingRelationship, ReportingRelationshipId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ReportingRelationship> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ReportingRelationshipId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.ManagerId).HasConversion(id => id.Value, value => new EmployeeId(value));

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });
    }
}
