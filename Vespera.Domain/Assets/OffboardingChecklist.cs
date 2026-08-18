using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Assets;

public sealed class OffboardingChecklistItem : ValueObject
{
    private OffboardingChecklistItem(string description, bool isComplete)
    {
        Description = description;
        IsComplete = isComplete;
    }

    public string Description { get; }

    public bool IsComplete { get; }

    public static OffboardingChecklistItem Create(string description) => new(description, false);

    internal OffboardingChecklistItem MarkComplete() => new(Description, true);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Description;
        yield return IsComplete;
    }
}

public readonly record struct OffboardingChecklistId(Guid Value)
{
    public static OffboardingChecklistId New() => new(Guid.NewGuid());
}

public sealed class OffboardingChecklist : AggregateRoot<OffboardingChecklistId>, ITenantScoped
{
    private readonly List<OffboardingChecklistItem> _items = [];

    private OffboardingChecklist(OffboardingChecklistId id, TenantId tenantId, EmployeeId employeeId)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public IReadOnlyList<OffboardingChecklistItem> Items => _items.AsReadOnly();

    public bool IsComplete => _items.Count > 0 && _items.All(item => item.IsComplete);

    public static OffboardingChecklist Create(TenantId tenantId, EmployeeId employeeId, IEnumerable<string> taskDescriptions)
    {
        var checklist = new OffboardingChecklist(OffboardingChecklistId.New(), tenantId, employeeId);

        foreach (var description in taskDescriptions)
        {
            checklist._items.Add(OffboardingChecklistItem.Create(description));
        }

        return checklist;
    }

    public Result CompleteItem(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return Result.Failure(Error.NotFound("offboarding_checklist.item_not_found", "Checklist item not found."));
        }

        if (_items[index].IsComplete)
        {
            return Result.Failure(Error.Conflict("offboarding_checklist.already_complete", "This item is already complete."));
        }

        _items[index] = _items[index].MarkComplete();
        return Result.Success();
    }
}
