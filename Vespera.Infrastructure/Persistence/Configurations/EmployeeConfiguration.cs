using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// Everything about <see cref="Employee"/> except <see cref="Employee.Pan"/> and
/// <see cref="Employee.BankAccount"/> — those two are encrypted at rest via
/// <c>IPiiProtector</c>, which this assembly-scanned, parameterless-constructed configuration has
/// no way to receive. <see cref="VesperaDbContext.OnModelCreating"/> configures those two columns
/// explicitly, immediately after applying this configuration.
/// </summary>
public sealed class EmployeeConfiguration : TenantScopedEntityConfiguration<Employee, EmployeeId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EmployeeId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Code)
            .HasConversion(code => code.Value, value => EmployeeCode.Create(value).Value)
            .IsRequired()
            .HasMaxLength(32);
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(128);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(128);

        builder.Property(e => e.WorkEmail)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value).Value)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(e => e.Phone)
            .HasConversion(phone => phone.Value, value => PhoneNumber.Create(value).Value)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.DateOfBirth).IsRequired();
        builder.Property(e => e.DateOfJoining).IsRequired();

        builder.Property(e => e.DepartmentId).HasConversion(id => id.Value, value => new DepartmentId(value));
        builder.Property(e => e.DesignationId).HasConversion(id => id.Value, value => new DesignationId(value));
        builder.Property(e => e.LocationId).HasConversion(id => id.Value, value => new LocationId(value));

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.ExitDate);
        builder.Property(e => e.ExitReason).HasConversion<string>().HasMaxLength(32);

        builder.OwnsMany(e => e.EmploymentHistory, history =>
        {
            history.ToTable("EmployeeEmploymentHistory");
            history.HasKey(h => h.Id);
            history.Property(h => h.Id)
                .HasConversion(id => id.Value, value => new EmploymentHistoryId(value))
                .ValueGeneratedNever();
            history.Property(h => h.DepartmentId).HasConversion(id => id.Value, value => new DepartmentId(value));
            history.Property(h => h.DesignationId).HasConversion(id => id.Value, value => new DesignationId(value));
            history.Property(h => h.LocationId).HasConversion(id => id.Value, value => new LocationId(value));
            history.Property(h => h.EffectiveFrom).IsRequired();
            history.Property(h => h.ChangeReason).HasConversion<string>().HasMaxLength(32);
        });

        builder.OwnsMany(e => e.Documents, documents =>
        {
            documents.ToTable("EmployeeDocuments");
            documents.HasKey(d => d.Id);
            documents.Property(d => d.Id)
                .HasConversion(id => id.Value, value => new EmployeeDocumentId(value))
                .ValueGeneratedNever();
            documents.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(32);
            documents.Property(d => d.FileReference).IsRequired().HasMaxLength(1024);
            documents.Property(d => d.UploadedAt).IsRequired();
            documents.Property(d => d.VerificationStatus).HasConversion<string>().HasMaxLength(32);
            documents.Property(d => d.RejectionReason).HasMaxLength(1024);
        });

        builder.OwnsMany(e => e.ConsentRecords, consents =>
        {
            consents.ToTable("EmployeeConsentRecords");
            consents.HasKey(c => c.Id);
            consents.Property(c => c.Id)
                .HasConversion(id => id.Value, value => new ConsentRecordId(value))
                .ValueGeneratedNever();
            consents.Property(c => c.ConsentType).HasConversion<string>().HasMaxLength(32);
            consents.Property(c => c.Granted).IsRequired();
            consents.Property(c => c.GrantedAt).IsRequired();
            consents.Property(c => c.WithdrawnAt);
        });
    }
}
