using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Assets;

public readonly record struct SoftwareLicenseAllocationId(Guid Value)
{
    public static SoftwareLicenseAllocationId New() => new(Guid.NewGuid());
}

/// <summary>Which employee holds one seat of a <see cref="SoftwareLicense"/> — the license's own
/// <see cref="SoftwareLicense.SeatsUsed"/> counter is the source of truth for the aggregate seat
/// count, this is the per-employee record needed for the unused-seat report and to release seats
/// automatically on employee exit.</summary>
public sealed class SoftwareLicenseAllocation : AggregateRoot<SoftwareLicenseAllocationId>, ITenantScoped
{
    private SoftwareLicenseAllocation(
        SoftwareLicenseAllocationId id, TenantId tenantId, SoftwareLicenseId licenseId, EmployeeId employeeId, DateTimeOffset allocatedAt)
        : base(id)
    {
        TenantId = tenantId;
        LicenseId = licenseId;
        EmployeeId = employeeId;
        AllocatedAt = allocatedAt;
    }

    public TenantId TenantId { get; }

    public SoftwareLicenseId LicenseId { get; }

    public EmployeeId EmployeeId { get; }

    public DateTimeOffset AllocatedAt { get; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public bool IsActive => ReleasedAt is null;

    public static SoftwareLicenseAllocation Allocate(
        TenantId tenantId, SoftwareLicenseId licenseId, EmployeeId employeeId, DateTimeOffset occurredOn) =>
        new(SoftwareLicenseAllocationId.New(), tenantId, licenseId, employeeId, occurredOn);

    public Result Release(DateTimeOffset occurredOn)
    {
        if (ReleasedAt is not null)
        {
            return Result.Failure(Error.Conflict("software_license_allocation.already_released", "This allocation has already been released."));
        }

        ReleasedAt = occurredOn;
        return Result.Success();
    }
}
