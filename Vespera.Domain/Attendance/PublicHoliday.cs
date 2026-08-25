using Vespera.Domain.Common;

namespace Vespera.Domain.Attendance;

public readonly record struct PublicHolidayId(Guid Value)
{
    public static PublicHolidayId New() => new(Guid.NewGuid());
}

/// <summary>
/// A tenant's holiday calendar, one row per observed date. Consumed by
/// <see cref="Services.BusinessHoursCalculator"/> so SLA due-date math (and, later, other
/// business-hours-aware calculations) skips these dates the same way it skips weekends.
/// </summary>
public sealed class PublicHoliday : AggregateRoot<PublicHolidayId>, ITenantScoped
{
    private PublicHoliday(PublicHolidayId id, TenantId tenantId, DateOnly date, string name)
        : base(id)
    {
        TenantId = tenantId;
        Date = date;
        Name = name;
    }

    public TenantId TenantId { get; }

    public DateOnly Date { get; }

    public string Name { get; }

    public static Result<PublicHoliday> Create(TenantId tenantId, DateOnly date, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<PublicHoliday>(Error.Validation("public_holiday.name_required", "Holiday name is required."));
        }

        return Result.Success(new PublicHoliday(PublicHolidayId.New(), tenantId, date, name.Trim()));
    }
}
