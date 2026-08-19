using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Workspace;

public readonly record struct AnnouncementReceiptId(Guid Value)
{
    public static AnnouncementReceiptId New() => new(Guid.NewGuid());
}

public sealed class AnnouncementReceipt : AggregateRoot<AnnouncementReceiptId>, ITenantScoped
{
    private AnnouncementReceipt(AnnouncementReceiptId id, TenantId tenantId, AnnouncementId announcementId, EmployeeId employeeId)
        : base(id)
    {
        TenantId = tenantId;
        AnnouncementId = announcementId;
        EmployeeId = employeeId;
    }

    public TenantId TenantId { get; }

    public AnnouncementId AnnouncementId { get; }

    public EmployeeId EmployeeId { get; }

    public DateTimeOffset? AcknowledgedAt { get; private set; }

    public static AnnouncementReceipt Create(TenantId tenantId, AnnouncementId announcementId, EmployeeId employeeId) =>
        new(AnnouncementReceiptId.New(), tenantId, announcementId, employeeId);

    public Result Acknowledge(DateTimeOffset occurredOn)
    {
        if (AcknowledgedAt is not null)
        {
            return Result.Failure(Error.Conflict("announcement_receipt.already_acknowledged", "Already acknowledged."));
        }

        AcknowledgedAt = occurredOn;
        return Result.Success();
    }
}
