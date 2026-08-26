using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class InvestmentDeclarationWindowConfiguration : IEntityTypeConfiguration<InvestmentDeclarationWindow>
{
    public void Configure(EntityTypeBuilder<InvestmentDeclarationWindow> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new InvestmentDeclarationWindowId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.FinancialYear).IsRequired().HasMaxLength(16);
        builder.Property(e => e.OpenFrom).IsRequired();
        builder.Property(e => e.LockAt).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.FinancialYear }).IsUnique();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);
    }
}
