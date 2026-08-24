using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Infrastructure.Persistence.Configurations.Recruitment;

/// <summary>Interview is AggregateRoot+ITenantScoped, not AuditableTenantAggregateRoot, so it's
/// configured directly — mirrors ExpenseClaimConfiguration's shape.</summary>
public sealed class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new InterviewId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.CandidateId).HasConversion(id => id.Value, value => new CandidateId(value));
        builder.Property(e => e.PipelineStageId).HasConversion(id => id.Value, value => new PipelineStageId(value));
        builder.Property(e => e.ScheduledAt).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Feedback).HasMaxLength(2048);
        builder.Property(e => e.Rating);

        // InterviewerIds is a plain IReadOnlyList<EmployeeId>, not an owned entity collection —
        // there is no existing precedent in this codebase for mapping a raw scalar list. Interview
        // has no parameterless constructor, so EF must materialize it via constructor binding,
        // which requires every constructor parameter (including interviewerIds) to resolve to a
        // mapped property of a matching CLR type — mapping the real getter-only property directly
        // (rather than the backing field under a different name/type) is what satisfies that:
        // the converted value is supplied as a constructor argument at read time, and since
        // nothing ever mutates this collection after Schedule(), EF never needs to write it back
        // through a setter.
        builder.Property(e => e.InterviewerIds)
            .HasConversion(EmployeeIdListConverter.Instance, EmployeeIdListConverter.Comparer)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);

        // InterviewScorecard is a plain record with no natural key — same shadow-key trick as
        // AssetConditionReport in Phase 11b's AssetAssignmentConfiguration.
        builder.OwnsMany(e => e.Scorecards, scorecards =>
        {
            scorecards.ToTable("InterviewScorecards");
            scorecards.Property<Guid>("Id").ValueGeneratedOnAdd();
            scorecards.HasKey("Id");

            scorecards.Property(s => s.InterviewerId).HasConversion(id => id.Value, value => new EmployeeId(value));
            scorecards.Property(s => s.Rating).IsRequired();
            scorecards.Property(s => s.Notes).HasMaxLength(1024);
            scorecards.Property(s => s.SubmittedAt).IsRequired();
        });
    }

    private static class EmployeeIdListConverter
    {
        public static readonly ValueConverter<IReadOnlyList<EmployeeId>, string> Instance = new(
            list => string.Join(',', list.Select(id => id.Value)),
            value => Parse(value));

        public static readonly ValueComparer<IReadOnlyList<EmployeeId>> Comparer = new(
            (left, right) => (left ?? Array.Empty<EmployeeId>()).SequenceEqual(right ?? Array.Empty<EmployeeId>()),
            list => list.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.Value)),
            list => list.ToList());

        private static List<EmployeeId> Parse(string value) =>
            string.IsNullOrEmpty(value)
                ? []
                : value.Split(',').Select(part => new EmployeeId(Guid.Parse(part))).ToList();
    }
}
