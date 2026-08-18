using Vespera.Domain.Common;

namespace Vespera.Domain.Assets;

public readonly record struct SoftwareLicenseId(Guid Value)
{
    public static SoftwareLicenseId New() => new(Guid.NewGuid());
}

public sealed class SoftwareLicense : AuditableTenantAggregateRoot<SoftwareLicenseId>
{
    private SoftwareLicense(
        SoftwareLicenseId id, TenantId tenantId, string productName, int seatCount, DateOnly? expiresAt,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        ProductName = productName;
        SeatCount = seatCount;
        ExpiresAt = expiresAt;
        SeatsUsed = 0;
    }

    public string ProductName { get; private set; }

    public int SeatCount { get; private set; }

    public int SeatsUsed { get; private set; }

    public DateOnly? ExpiresAt { get; private set; }

    public static Result<SoftwareLicense> Create(
        TenantId tenantId, string productName, int seatCount, DateOnly? expiresAt, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return Result.Failure<SoftwareLicense>(Error.Validation("software_license.product_name_required", "Product name is required."));
        }

        if (seatCount <= 0)
        {
            return Result.Failure<SoftwareLicense>(Error.Validation("software_license.invalid_seat_count", "Seat count must be positive."));
        }

        return Result.Success(new SoftwareLicense(SoftwareLicenseId.New(), tenantId, productName.Trim(), seatCount, expiresAt, occurredOn, createdBy));
    }

    public Result AssignSeat()
    {
        if (SeatsUsed >= SeatCount)
        {
            return Result.Failure(Error.Conflict("software_license.no_seats_available", "No seats are available."));
        }

        SeatsUsed++;
        return Result.Success();
    }

    public Result ReleaseSeat()
    {
        if (SeatsUsed <= 0)
        {
            return Result.Failure(Error.Conflict("software_license.no_seats_in_use", "No seats are currently in use."));
        }

        SeatsUsed--;
        return Result.Success();
    }
}
