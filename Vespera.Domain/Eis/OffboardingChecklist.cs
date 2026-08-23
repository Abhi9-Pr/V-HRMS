using Vespera.Domain.Common;

namespace Vespera.Domain.Eis;

public readonly record struct OffboardingChecklistId(Guid Value)
{
    public static OffboardingChecklistId New() => new(Guid.NewGuid());
}

public enum ChecklistItemStatus
{
    Pending,
    Done,
}

/// <summary>
/// Tracks the fixed set of exit-process steps for one employee, initiated automatically when
/// <see cref="Employee.Exit"/> raises <see cref="Events.EmployeeExited"/> — see
/// <c>Vespera.Application.Features.Eis.EmployeeExitedDomainEventHandler</c>. Asset recovery and
/// final settlement are explicit placeholders: this checklist only records that the step
/// happened, it never calls into the (separate, out-of-scope-here) Assets/Payroll modules.
/// </summary>
public sealed class OffboardingChecklist : AuditableTenantAggregateRoot<OffboardingChecklistId>
{
    private OffboardingChecklist(
        OffboardingChecklistId id, TenantId tenantId, EmployeeId employeeId, DateOnly exitDate,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        EmployeeId = employeeId;
        ExitDate = exitDate;
        AccessRevokedStatus = ChecklistItemStatus.Pending;
        AssetsRecoveredStatus = ChecklistItemStatus.Pending;
        FinalSettlementStatus = ChecklistItemStatus.Pending;
    }

    public EmployeeId EmployeeId { get; }

    public DateOnly ExitDate { get; }

    public ChecklistItemStatus AccessRevokedStatus { get; private set; }

    public DateTimeOffset? AccessRevokedAt { get; private set; }

    public ChecklistItemStatus AssetsRecoveredStatus { get; private set; }

    public DateTimeOffset? AssetsRecoveredAt { get; private set; }

    public ChecklistItemStatus FinalSettlementStatus { get; private set; }

    public DateTimeOffset? FinalSettlementAt { get; private set; }

    public static Result<OffboardingChecklist> Initiate(
        TenantId tenantId, EmployeeId employeeId, DateOnly exitDate, DateTimeOffset createdAt, string createdBy) =>
        Result.Success(new OffboardingChecklist(OffboardingChecklistId.New(), tenantId, employeeId, exitDate, createdAt, createdBy));

    public Result MarkAccessRevoked(DateTimeOffset completedAt, string modifiedBy)
    {
        if (AccessRevokedStatus == ChecklistItemStatus.Done)
        {
            return Result.Failure(Error.Conflict("offboarding_checklist.access_already_revoked", "Access has already been revoked."));
        }

        AccessRevokedStatus = ChecklistItemStatus.Done;
        AccessRevokedAt = completedAt;
        Touch(completedAt, modifiedBy);
        return Result.Success();
    }

    public Result MarkAssetsRecovered(DateTimeOffset completedAt, string modifiedBy)
    {
        if (AssetsRecoveredStatus == ChecklistItemStatus.Done)
        {
            return Result.Failure(Error.Conflict("offboarding_checklist.assets_already_recovered", "Assets have already been recorded as recovered."));
        }

        AssetsRecoveredStatus = ChecklistItemStatus.Done;
        AssetsRecoveredAt = completedAt;
        Touch(completedAt, modifiedBy);
        return Result.Success();
    }

    public Result MarkFinalSettlementProcessed(DateTimeOffset completedAt, string modifiedBy)
    {
        if (FinalSettlementStatus == ChecklistItemStatus.Done)
        {
            return Result.Failure(Error.Conflict("offboarding_checklist.final_settlement_already_processed", "Final settlement has already been processed."));
        }

        FinalSettlementStatus = ChecklistItemStatus.Done;
        FinalSettlementAt = completedAt;
        Touch(completedAt, modifiedBy);
        return Result.Success();
    }
}
