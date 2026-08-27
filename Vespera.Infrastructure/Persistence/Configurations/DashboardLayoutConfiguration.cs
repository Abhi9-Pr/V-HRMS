using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Workspace;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary><see cref="DashboardLayout.Widgets"/> is owned exactly like <c>LeaveBalance.Entries</c>
/// (see <c>LeaveBalanceConfiguration</c>) — a replace-in-place child collection with no identity
/// meaningful outside its parent.</summary>
public sealed class DashboardLayoutConfiguration : IEntityTypeConfiguration<DashboardLayout>
{
    public void Configure(EntityTypeBuilder<DashboardLayout> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new DashboardLayoutId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.UserId).HasConversion(id => id.Value, value => new UserId(value)).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();

        builder.OwnsMany(e => e.Widgets, widgets =>
        {
            widgets.ToTable("DashboardWidgetPreferences");
            widgets.HasKey(w => w.Id);
            widgets.Property(w => w.Id)
                .HasConversion(id => id.Value, value => new WidgetPreferenceId(value))
                .ValueGeneratedNever();

            widgets.Property(w => w.WidgetKey).IsRequired().HasMaxLength(64);
            widgets.Property(w => w.SortOrder).IsRequired();
            widgets.Property(w => w.IsVisible).IsRequired();
            widgets.Property(w => w.Size).HasConversion<string>().HasMaxLength(16).IsRequired();

            widgets.HasIndex("DashboardLayoutId", nameof(Domain.Workspace.WidgetPreference.WidgetKey)).IsUnique();
        });
    }
}
