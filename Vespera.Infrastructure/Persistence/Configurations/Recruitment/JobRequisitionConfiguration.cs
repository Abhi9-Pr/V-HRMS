using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Infrastructure.Persistence.Configurations.Recruitment;

public sealed class JobRequisitionConfiguration : TenantScopedEntityConfiguration<JobRequisition, JobRequisitionId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<JobRequisition> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new JobRequisitionId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder.Property(e => e.DepartmentId).HasConversion(id => id.Value, value => new DepartmentId(value));
        builder.Property(e => e.OpeningsCount).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.ApprovalStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.IsPublished).IsRequired();
        builder.Property(e => e.RejectionReason).HasMaxLength(1024);

        // PipelineStage is a real Entity<PipelineStageId> (has its own Id), so — unlike the
        // keyless owned collections elsewhere in Phase 11 — it needs no shadow key trick.
        builder.OwnsMany(e => e.Stages, stages =>
        {
            stages.ToTable("PipelineStages");
            stages.HasKey(s => s.Id);
            stages.Property(s => s.Id)
                .HasConversion(id => id.Value, value => new PipelineStageId(value))
                .ValueGeneratedNever();

            stages.Property(s => s.Name).IsRequired().HasMaxLength(100);
            stages.Property(s => s.SequenceNumber).IsRequired();
        });
    }
}
