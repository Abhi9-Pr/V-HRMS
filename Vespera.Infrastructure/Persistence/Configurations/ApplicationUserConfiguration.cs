using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Infrastructure.Identity;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.NormalizedUserName).IsRequired().HasMaxLength(256);
        builder.HasIndex(e => e.NormalizedUserName).IsUnique();

        builder.Property(e => e.Email).IsRequired().HasMaxLength(254);
        builder.Property(e => e.NormalizedEmail).IsRequired().HasMaxLength(254);
        builder.HasIndex(e => e.NormalizedEmail).IsUnique();

        builder.Property(e => e.PasswordHash);
        builder.Property(e => e.SecurityStamp).IsRequired().HasMaxLength(64);
        builder.Property(e => e.ConcurrencyStamp).IsRequired().HasMaxLength(64).IsConcurrencyToken();

        builder.Property(e => e.AuthenticatorKey).HasMaxLength(256);
        builder.Property(e => e.RecoveryCodesConcatenated);
    }
}
