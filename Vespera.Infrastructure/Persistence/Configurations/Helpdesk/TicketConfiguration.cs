using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Infrastructure.Persistence.Configurations.Helpdesk;

/// <summary>Ticket is AggregateRoot+ITenantScoped, not AuditableTenantAggregateRoot, so it's
/// configured directly — mirrors ExpenseClaimConfiguration's shape.</summary>
public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    // Unit Separator (code point 31) as the join delimiter for AttachmentReferencesConverter — a
    // control character with no plausible presence inside a real storage-key/file-reference
    // string, unlike '|' or ','.
    private const char AttachmentReferenceDelimiter = (char)31;

    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new TicketId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.RaisedBy).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.Property(e => e.CategoryId).HasConversion(id => id.Value, value => new TicketCategoryId(value));
        builder.Property(e => e.SlaPolicyId).HasConversion(id => id.Value, value => new SlaPolicyId(value));
        builder.Property(e => e.AssignedTo).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null, value => value.HasValue ? new EmployeeId(value.Value) : (EmployeeId?)null);

        builder.Property(e => e.Subject).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(4096);
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.RaisedAt).IsRequired();
        builder.Property(e => e.DueAt).IsRequired();
        builder.Property(e => e.ResolvedAt);
        builder.Property(e => e.SlaBreachNotified).IsRequired();
        builder.Property(e => e.SlaWarningNotified).IsRequired();
        builder.Property(e => e.SatisfactionRating);

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);

        builder.OwnsMany(e => e.Comments, comments =>
        {
            comments.ToTable("TicketComments");
            comments.HasKey(c => c.Id);
            comments.Property(c => c.Id)
                .HasConversion(id => id.Value, value => new TicketCommentId(value))
                .ValueGeneratedNever();

            comments.Property(c => c.AuthorId).HasConversion(id => id.Value, value => new EmployeeId(value));
            comments.Property(c => c.Body).IsRequired().HasMaxLength(4096);
            comments.Property(c => c.IsInternal).IsRequired();
            comments.Property(c => c.CreatedAt).IsRequired();
            comments.Property(c => c.ParentCommentId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null, value => value.HasValue ? new TicketCommentId(value.Value) : (TicketCommentId?)null);

            // AttachmentReferences is a plain IReadOnlyList<string>, not an owned entity collection
            // — same situation Interview.InterviewerIds hit in Phase 11c: TicketComment has no
            // parameterless constructor, so EF must materialize it via constructor binding, which
            // requires mapping the real public property directly (typed to match the constructor
            // parameter) rather than a private field under a different name.
            comments.Property(c => c.AttachmentReferences)
                .HasConversion(AttachmentReferencesConverter.Instance, AttachmentReferencesConverter.Comparer)
                .HasMaxLength(4000);
        });
    }

    private static class AttachmentReferencesConverter
    {
        public static readonly ValueConverter<IReadOnlyList<string>, string> Instance = new(
            list => string.Join(AttachmentReferenceDelimiter, list),
            value => Parse(value));

        public static readonly ValueComparer<IReadOnlyList<string>> Comparer = new(
            (left, right) => (left ?? Array.Empty<string>()).SequenceEqual(right ?? Array.Empty<string>()),
            list => list.Aggregate(0, (hash, reference) => HashCode.Combine(hash, reference)),
            list => list.ToList());

        private static List<string> Parse(string value) =>
            string.IsNullOrEmpty(value) ? [] : value.Split(AttachmentReferenceDelimiter).ToList();
    }
}
