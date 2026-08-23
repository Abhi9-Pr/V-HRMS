using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Attendance;

public readonly record struct HolidayId(Guid Value)
{
    public static HolidayId New() => new(Guid.NewGuid());
}

/// <summary>One holiday observed at one <see cref="LocationId"/>. A holiday observed at every
/// location still needs one row per location — there is no cross-location "applies everywhere"
/// flag, matching this codebase's preference for explicit data over implicit defaults.</summary>
public sealed class Holiday : AuditableTenantAggregateRoot<HolidayId>
{
    private Holiday(
        HolidayId id, TenantId tenantId, LocationId locationId, DateOnly date, string name,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        LocationId = locationId;
        Date = date;
        Name = name;
    }

    public LocationId LocationId { get; }

    public DateOnly Date { get; private set; }

    public string Name { get; private set; }

    public static Result<Holiday> Create(
        TenantId tenantId, LocationId locationId, DateOnly date, string name,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Holiday>(Error.Validation("holiday.name_required", "Holiday name is required."));
        }

        return Result.Success(new Holiday(HolidayId.New(), tenantId, locationId, date, name.Trim(), occurredOn, createdBy));
    }

    public Result Rename(string name, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("holiday.name_required", "Holiday name is required."));
        }

        Name = name.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Reschedule(DateOnly date, DateTimeOffset occurredOn, string modifiedBy)
    {
        Date = date;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
