using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Workspace;

public readonly record struct CelebrationId(Guid Value)
{
    public static CelebrationId New() => new(Guid.NewGuid());
}

public enum CelebrationType
{
    Birthday,
    WorkAnniversary,
}

public sealed class Celebration : AggregateRoot<CelebrationId>, ITenantScoped
{
    private Celebration(CelebrationId id, TenantId tenantId, EmployeeId employeeId, CelebrationType celebrationType, DateOnly date)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        CelebrationType = celebrationType;
        Date = date;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public CelebrationType CelebrationType { get; }

    public DateOnly Date { get; private set; }

    public static Celebration Create(TenantId tenantId, EmployeeId employeeId, CelebrationType celebrationType, DateOnly date) =>
        new(CelebrationId.New(), tenantId, employeeId, celebrationType, date);

    public Result Reschedule(DateOnly date)
    {
        Date = date;
        return Result.Success();
    }
}
