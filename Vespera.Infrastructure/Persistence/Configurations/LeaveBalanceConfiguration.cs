using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="LeaveBalance"/> is a plain <c>AggregateRoot&lt;TId&gt;</c> + <c>ITenantScoped</c>. Its
/// <see cref="LeaveBalance.Available"/>/<c>Accrued</c>/<c>Used</c>/<c>CarriedForward</c> are computed
/// (unmapped) properties — the only persisted state is the owned <see cref="LeaveLedgerEntry"/>
/// collection, exactly mirroring <c>AttendanceDay.Punches</c>.
/// </summary>
public sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new LeaveBalanceId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.LeaveTypeId).HasConversion(id => id.Value, value => new LeaveTypeId(value));
        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.LeaveTypeId }).IsUnique();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.Available);
        builder.Ignore(e => e.Accrued);
        builder.Ignore(e => e.CarriedForward);
        builder.Ignore(e => e.Used);

        builder.OwnsMany(e => e.Entries, entries =>
        {
            entries.ToTable("LeaveLedgerEntries");
            entries.HasKey(e => e.Id);
            entries.Property(e => e.Id)
                .HasConversion(id => id.Value, value => new LeaveLedgerEntryId(value))
                .ValueGeneratedNever();

            entries.Property(e => e.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
            entries.Property(e => e.Direction).HasConversion<string>().HasMaxLength(8).IsRequired();
            entries.Property(e => e.Amount).HasPrecision(9, 2);
            entries.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entries.Property(e => e.SourceType).HasMaxLength(64);
            entries.Property(e => e.SourceId);
            entries.Property(e => e.PeriodKey).HasMaxLength(16);
            entries.Property(e => e.OccurredOn).IsRequired();
            entries.Property(e => e.PostedBy).IsRequired().HasMaxLength(256);

            entries.HasIndex("LeaveBalanceId", nameof(LeaveLedgerEntry.SourceType), nameof(LeaveLedgerEntry.SourceId));
            entries.HasIndex("LeaveBalanceId", nameof(LeaveLedgerEntry.PeriodKey));
        });
    }
}
