using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Helpdesk;

public readonly record struct TicketCategoryId(Guid Value)
{
    public static TicketCategoryId New() => new(Guid.NewGuid());
}

public sealed class TicketCategory : AuditableTenantAggregateRoot<TicketCategoryId>
{
    private TicketCategory(
        TicketCategoryId id, TenantId tenantId, string name, DepartmentId departmentId, SlaPolicyId? defaultSlaPolicyId,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        DepartmentId = departmentId;
        DefaultSlaPolicyId = defaultSlaPolicyId;
    }

    public string Name { get; private set; }

    public DepartmentId DepartmentId { get; }

    public SlaPolicyId? DefaultSlaPolicyId { get; private set; }

    public static Result<TicketCategory> Create(
        TenantId tenantId, string name, DepartmentId departmentId, SlaPolicyId? defaultSlaPolicyId, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<TicketCategory>(Error.Validation("ticket_category.name_required", "Category name is required."));
        }

        return Result.Success(new TicketCategory(TicketCategoryId.New(), tenantId, name.Trim(), departmentId, defaultSlaPolicyId, occurredOn, createdBy));
    }

    public Result Rename(string name, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("ticket_category.name_required", "Category name is required."));
        }

        Name = name.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result AssignDefaultSla(SlaPolicyId slaPolicyId, DateTimeOffset occurredOn, string modifiedBy)
    {
        DefaultSlaPolicyId = slaPolicyId;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
