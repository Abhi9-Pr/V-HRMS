using Vespera.Domain.Common;

namespace Vespera.Domain.Payroll;

public readonly record struct PayrollSettingsId(Guid Value)
{
    public static PayrollSettingsId New() => new(Guid.NewGuid());
}

/// <summary>Per-tenant payroll configuration: the day of the month attendance freezes by default
/// (<see cref="PayrollRun.FreezeAttendance"/> requires an override reason before this day) and the
/// variance-report threshold (<c>GetPayrollRunVarianceQuery</c> flags any employee whose net moves
/// more than this percent versus the prior finalized cycle).</summary>
public sealed class PayrollSettings : AggregateRoot<PayrollSettingsId>, ITenantScoped
{
    private PayrollSettings(PayrollSettingsId id, TenantId tenantId, int attendanceFreezeDay, decimal varianceThresholdPercent)
        : base(id)
    {
        TenantId = tenantId;
        AttendanceFreezeDay = attendanceFreezeDay;
        VarianceThresholdPercent = varianceThresholdPercent;
    }

    public TenantId TenantId { get; }

    public int AttendanceFreezeDay { get; private set; }

    public decimal VarianceThresholdPercent { get; private set; }

    public static Result<PayrollSettings> Create(TenantId tenantId, int attendanceFreezeDay, decimal varianceThresholdPercent)
    {
        var validation = Validate(attendanceFreezeDay, varianceThresholdPercent);
        if (validation.IsFailure)
        {
            return Result.Failure<PayrollSettings>(validation.Error);
        }

        return Result.Success(new PayrollSettings(PayrollSettingsId.New(), tenantId, attendanceFreezeDay, varianceThresholdPercent));
    }

    public Result Update(int attendanceFreezeDay, decimal varianceThresholdPercent)
    {
        var validation = Validate(attendanceFreezeDay, varianceThresholdPercent);
        if (validation.IsFailure)
        {
            return validation;
        }

        AttendanceFreezeDay = attendanceFreezeDay;
        VarianceThresholdPercent = varianceThresholdPercent;
        return Result.Success();
    }

    private static Result Validate(int attendanceFreezeDay, decimal varianceThresholdPercent)
    {
        if (attendanceFreezeDay is < 1 or > 28)
        {
            return Result.Failure(Error.Validation(
                "payroll_settings.invalid_freeze_day", "Attendance freeze day must be between 1 and 28 (to stay valid in every month)."));
        }

        if (varianceThresholdPercent <= 0)
        {
            return Result.Failure(Error.Validation(
                "payroll_settings.invalid_variance_threshold", "Variance threshold percent must be positive."));
        }

        return Result.Success();
    }
}
