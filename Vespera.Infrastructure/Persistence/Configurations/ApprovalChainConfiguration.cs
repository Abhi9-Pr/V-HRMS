using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>ApprovalChain is AggregateRoot+ITenantScoped, not AuditableTenantAggregateRoot, so it's
/// configured directly (same shape as ExpenseClaimConfiguration). This is the generic engine
/// LeaveRequest and ExpenseClaim both submit through — first persisted by the Expenses slice
/// (11a), reusable unchanged by Leave later.</summary>
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
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.SubjectType).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.SubjectId).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.CurrentStepIndex).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);

        builder.OwnsMany(e => e.Steps, steps =>
        {
            steps.ToTable("ApprovalSteps");
            steps.HasKey(s => s.Id);
            steps.Property(s => s.Id)
                .HasConversion(id => id.Value, value => new ApprovalStepId(value))
                .ValueGeneratedNever();

            steps.Property(s => s.SequenceNumber).IsRequired();
            steps.Property(s => s.ApproverId).HasConversion(id => id.Value, value => new EmployeeId(value));
            steps.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);
            steps.Property(s => s.DecidedBy)
                .HasConversion(id => id != null ? id.Value.Value : (Guid?)null, value => value != null ? new EmployeeId(value.Value) : (EmployeeId?)null);
            steps.Property(s => s.DecidedAt);
            steps.Property(s => s.Comment).HasMaxLength(1024);
        });
    }
}
