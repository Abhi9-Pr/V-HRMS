using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="ApprovalChain"/> is a plain <c>AggregateRoot&lt;TId&gt;</c> + <c>ITenantScoped</c>,
/// owning its <see cref="ApprovalStep"/> collection exactly like <c>AttendanceDay.Punches</c>.
/// </summary>
public sealed class ApprovalChainConfiguration : IEntityTypeConfiguration<ApprovalChain>
{
    public void Configure(EntityTypeBuilder<ApprovalChain> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ApprovalChainId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.SubjectType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.SubjectId).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.SubjectType, e.SubjectId }).IsUnique();

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.CurrentStepIndex).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.CurrentStep);

        builder.OwnsMany(e => e.Steps, steps =>
        {
            steps.ToTable("ApprovalSteps");
            steps.HasKey(s => s.Id);
            steps.Property(s => s.Id)
                .HasConversion(id => id.Value, value => new ApprovalStepId(value))
                .ValueGeneratedNever();

            steps.Property(s => s.SequenceNumber).IsRequired();
            steps.Property(s => s.ApproverId).HasConversion(id => id.Value, value => new EmployeeId(value));
            steps.Property(s => s.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            steps.Property(s => s.DecidedBy).HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (EmployeeId?)null : new EmployeeId(value.Value));
            steps.Property(s => s.DecidedAt);
            steps.Property(s => s.Comment).HasMaxLength(1000);

            steps.HasIndex("ApprovalChainId", nameof(ApprovalStep.SequenceNumber)).IsUnique();
        });
    }
}
