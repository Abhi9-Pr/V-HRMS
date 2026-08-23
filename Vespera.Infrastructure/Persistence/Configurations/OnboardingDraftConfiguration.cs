using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class OnboardingDraftConfiguration : TenantScopedEntityConfiguration<OnboardingDraft, OnboardingDraftId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<OnboardingDraft> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(id => id.Value, value => new OnboardingDraftId(value))
            .ValueGeneratedNever();

        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(d => d.CurrentStep).HasConversion<string>().HasMaxLength(32);

        builder.Property(d => d.FirstName).HasMaxLength(128);
        builder.Property(d => d.LastName).HasMaxLength(128);

        builder.Property(d => d.WorkEmail)
            .HasConversion(email => email == null ? null : email.Value, value => value == null ? null : EmailAddress.Create(value).Value)
            .HasMaxLength(254);

        builder.Property(d => d.Phone)
            .HasConversion(phone => phone == null ? null : phone.Value, value => value == null ? null : PhoneNumber.Create(value).Value)
            .HasMaxLength(20);

        builder.Property(d => d.DateOfBirth);

        builder.Property(d => d.DepartmentId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, value => value == null ? (DepartmentId?)null : new DepartmentId(value.Value));
        builder.Property(d => d.DesignationId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, value => value == null ? (DesignationId?)null : new DesignationId(value.Value));
        builder.Property(d => d.LocationId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, value => value == null ? (LocationId?)null : new LocationId(value.Value));

        builder.Property(d => d.DateOfJoining);

        builder.Property(d => d.ConvertedEmployeeId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, value => value == null ? (EmployeeId?)null : new EmployeeId(value.Value));

        builder.OwnsMany(d => d.Documents, documents =>
        {
            documents.ToTable("OnboardingDraftDocuments");
            documents.HasKey(document => document.Id);
            documents.Property(document => document.Id)
                .HasConversion(id => id.Value, value => new EmployeeDocumentId(value))
                .ValueGeneratedNever();
            documents.Property(document => document.DocumentType).HasConversion<string>().HasMaxLength(32);
            documents.Property(document => document.FileReference).IsRequired().HasMaxLength(1024);
            documents.Property(document => document.UploadedAt).IsRequired();
            documents.Property(document => document.VerificationStatus).HasConversion<string>().HasMaxLength(32);
            documents.Property(document => document.RejectionReason).HasMaxLength(1024);
            documents.Property(document => document.ScanStatus).HasConversion<string>().HasMaxLength(32);
            documents.Property(document => document.OcrSuggestedFieldsJson);
            documents.Property(document => document.OcrConfidence);
            documents.Property(document => document.IsOcrConfirmed).IsRequired();
            documents.Property(document => document.ConfirmedFieldsJson);
            documents.Property(document => document.ConfirmedBy).HasMaxLength(256);
            documents.Property(document => document.ConfirmedAt);
        });

        builder.OwnsMany(d => d.ConsentRecords, consents =>
        {
            consents.ToTable("OnboardingDraftConsentRecords");
            consents.HasKey(consent => consent.Id);
            consents.Property(consent => consent.Id)
                .HasConversion(id => id.Value, value => new ConsentRecordId(value))
                .ValueGeneratedNever();
            consents.Property(consent => consent.ConsentType).HasConversion<string>().HasMaxLength(32);
            consents.Property(consent => consent.Granted).IsRequired();
            consents.Property(consent => consent.GrantedAt).IsRequired();
            consents.Property(consent => consent.WithdrawnAt);
        });
    }
}
