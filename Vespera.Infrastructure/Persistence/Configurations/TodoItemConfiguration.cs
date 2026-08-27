using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Infrastructure.Persistence.Configurations;

public sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new TodoItemId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasConversion(id => id.Value, value => new TenantId(value))
            .IsRequired();

        builder.Property(e => e.OwnerEmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder.Property(e => e.DueDate);
        builder.Property(e => e.Urgency).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.IsDone).IsRequired();

        builder.Property(e => e.RowVersion).IsConcurrencyToken();
        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.TenantId, e.OwnerEmployeeId, e.SortOrder });
    }
}
