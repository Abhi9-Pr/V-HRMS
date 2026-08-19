using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Workspace;

public readonly record struct EventRsvpId(Guid Value)
{
    public static EventRsvpId New() => new(Guid.NewGuid());
}

public enum RsvpResponse
{
    Yes,
    No,
    Maybe,
}

public sealed class EventRsvp : AggregateRoot<EventRsvpId>, ITenantScoped
{
    private EventRsvp(EventRsvpId id, TenantId tenantId, CorporateEventId corporateEventId, EmployeeId employeeId)
        : base(id)
    {
        TenantId = tenantId;
        CorporateEventId = corporateEventId;
        EmployeeId = employeeId;
    }

    public TenantId TenantId { get; }

    public CorporateEventId CorporateEventId { get; }

    public EmployeeId EmployeeId { get; }

    public RsvpResponse? Response { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    public static EventRsvp Create(TenantId tenantId, CorporateEventId corporateEventId, EmployeeId employeeId) =>
        new(EventRsvpId.New(), tenantId, corporateEventId, employeeId);

    public Result Respond(RsvpResponse response, DateTimeOffset occurredOn)
    {
        Response = response;
        RespondedAt = occurredOn;
        return Result.Success();
    }
}
