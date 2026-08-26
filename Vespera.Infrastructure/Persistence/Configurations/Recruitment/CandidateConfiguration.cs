using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations.Recruitment;

/// <summary>Candidate is AggregateRoot+ITenantScoped, not AuditableTenantAggregateRoot (no
/// audit/soft-delete columns), so it's configured directly rather than via
/// TenantScopedEntityConfiguration&lt;,&gt; — mirrors ExpenseClaimConfiguration's shape.</summary>
public sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new CandidateId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.JobRequisitionId).HasConversion(id => id.Value, value => new JobRequisitionId(value));
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(256);

        builder.Property(e => e.Email)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value).Value)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(e => e.Phone)
            .HasConversion(phone => phone.Value, value => PhoneNumber.Create(value).Value)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(e => e.CurrentPipelineStageId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, value => value == null ? (PipelineStageId?)null : new PipelineStageId(value.Value));

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
    }
}
