using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Expense;

public readonly record struct ExpensePolicyId(Guid Value)
{
    public static ExpensePolicyId New() => new(Guid.NewGuid());
}

public sealed class ExpensePolicy : AuditableTenantAggregateRoot<ExpensePolicyId>
{
    private ExpensePolicy(
        ExpensePolicyId id, TenantId tenantId, string category, Money maxAmountPerClaim, Money receiptRequiredAboveAmount,
        DesignationId? applicableDesignationId, ExpensePolicySeverity maxAmountSeverity, ExpensePolicySeverity receiptRequiredSeverity,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Category = category;
        MaxAmountPerClaim = maxAmountPerClaim;
        ReceiptRequiredAboveAmount = receiptRequiredAboveAmount;
        ApplicableDesignationId = applicableDesignationId;
        MaxAmountSeverity = maxAmountSeverity;
        ReceiptRequiredSeverity = receiptRequiredSeverity;
    }

    public string Category { get; private set; }

    public Money MaxAmountPerClaim { get; private set; }

    public Money ReceiptRequiredAboveAmount { get; private set; }

    /// <summary>Restricts this policy to employees holding this designation (the closest existing
    /// "grade" concept in this codebase). Null applies the policy to every designation.</summary>
    public DesignationId? ApplicableDesignationId { get; private set; }

    public ExpensePolicySeverity MaxAmountSeverity { get; private set; }

    public ExpensePolicySeverity ReceiptRequiredSeverity { get; private set; }

    public static Result<ExpensePolicy> Create(
        TenantId tenantId, string category, Money maxAmountPerClaim, Money receiptRequiredAboveAmount,
        DateTimeOffset occurredOn, string createdBy,
        DesignationId? applicableDesignationId = null,
        ExpensePolicySeverity maxAmountSeverity = ExpensePolicySeverity.Block,
        ExpensePolicySeverity receiptRequiredSeverity = ExpensePolicySeverity.Block)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Result.Failure<ExpensePolicy>(Error.Validation("expense_policy.category_required", "Category is required."));
        }

        return Result.Success(new ExpensePolicy(
            ExpensePolicyId.New(), tenantId, category.Trim(), maxAmountPerClaim, receiptRequiredAboveAmount,
            applicableDesignationId, maxAmountSeverity, receiptRequiredSeverity, occurredOn, createdBy));
    }

    public Result UpdateLimits(
        Money maxAmountPerClaim, Money receiptRequiredAboveAmount, DesignationId? applicableDesignationId,
        ExpensePolicySeverity maxAmountSeverity, ExpensePolicySeverity receiptRequiredSeverity,
        DateTimeOffset occurredOn, string modifiedBy)
    {
        MaxAmountPerClaim = maxAmountPerClaim;
        ReceiptRequiredAboveAmount = receiptRequiredAboveAmount;
        ApplicableDesignationId = applicableDesignationId;
        MaxAmountSeverity = maxAmountSeverity;
        ReceiptRequiredSeverity = receiptRequiredSeverity;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
