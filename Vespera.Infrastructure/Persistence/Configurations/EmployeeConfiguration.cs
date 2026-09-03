using System.Globalization;
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

        // GetAttendanceGridQueryHandler filters by these when a caller scopes the grid to one
        // department/location — confirmed via EXPLAIN ANALYZE against a 5,005-employee database
        // that without these, both filters fall back to a full sequential scan of Employee
        // (see docs/performance.md).
        builder.HasIndex(e => new { e.TenantId, e.DepartmentId });
        builder.HasIndex(e => new { e.TenantId, e.LocationId });

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.ExitDate);
        builder.Property(e => e.ExitReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Gender).HasConversion<string>().HasMaxLength(16);

        // A plain scalar conversion, not OwnsOne: EF Core can't bind an owned-navigation-typed
        // constructor parameter when materializing the owner via its (required, private)
        // constructor — see StatutoryRuleSetConfiguration/PayrollRunConfiguration for the same pattern.
        builder.Property(e => e.CurrentAnnualCtc)
            .HasConversion(
                ctc => ctc == null ? null : $"{ctc.Amount.ToString(CultureInfo.InvariantCulture)}|{ctc.Currency}",
                value => ParseMoney(value))
            .HasMaxLength(64);

        // A plain scalar conversion, not OwnsOne: EF Core can't bind an owned-navigation-typed
        // constructor parameter when materializing the owner via its (required, private)
        // constructor — see StatutoryRuleSetConfiguration/PayrollRunConfiguration for the same pattern.
        builder.Property(e => e.CurrentAnnualCtc)
            .HasConversion(
                ctc => ctc == null ? null : $"{ctc.Amount.ToString(CultureInfo.InvariantCulture)}|{ctc.Currency}",
                value => ParseMoney(value))
            .HasMaxLength(64);

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
            documents.Property(d => d.ScanStatus).HasConversion<string>().HasMaxLength(32);
            documents.Property(d => d.OcrSuggestedFieldsJson);
            documents.Property(d => d.OcrConfidence);
            documents.Property(d => d.IsOcrConfirmed).IsRequired();
            documents.Property(d => d.ConfirmedFieldsJson);
            documents.Property(d => d.ConfirmedBy).HasMaxLength(256);
            documents.Property(d => d.ConfirmedAt);
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

    private static Money? ParseMoney(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var parts = value.Split('|');
        return Money.Of(decimal.Parse(parts[0], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[1]));
    }
}
