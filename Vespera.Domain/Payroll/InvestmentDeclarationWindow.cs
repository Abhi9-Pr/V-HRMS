using Vespera.Domain.Common;

namespace Vespera.Domain.Payroll;

public readonly record struct InvestmentDeclarationWindowId(Guid Value)
{
    public static InvestmentDeclarationWindowId New() => new(Guid.NewGuid());
}

/// <summary>Per-tenant, per-financial-year submission window for investment declarations —
/// <see cref="InvestmentDeclaration.AddLine"/> and <see cref="InvestmentDeclaration.Submit"/> are
/// both checked against <see cref="IsOpenOn"/>, so a declaration cannot be created or edited before
/// <see cref="OpenFrom"/> or on/after <see cref="LockAt"/>.</summary>
public sealed class InvestmentDeclarationWindow : AggregateRoot<InvestmentDeclarationWindowId>, ITenantScoped
{
    private InvestmentDeclarationWindow(
        InvestmentDeclarationWindowId id, TenantId tenantId, string financialYear, DateOnly openFrom, DateOnly lockAt)
        : base(id)
    {
        TenantId = tenantId;
        FinancialYear = financialYear;
        OpenFrom = openFrom;
        LockAt = lockAt;
    }

    public TenantId TenantId { get; }

    public string FinancialYear { get; }

    public DateOnly OpenFrom { get; }

    public DateOnly LockAt { get; }

    public static Result<InvestmentDeclarationWindow> Create(TenantId tenantId, string financialYear, DateOnly openFrom, DateOnly lockAt)
    {
        if (string.IsNullOrWhiteSpace(financialYear))
        {
            return Result.Failure<InvestmentDeclarationWindow>(
                Error.Validation("investment_declaration_window.fy_required", "Financial year is required."));
        }

        if (lockAt <= openFrom)
        {
            return Result.Failure<InvestmentDeclarationWindow>(
                Error.Validation("investment_declaration_window.invalid_range", "The lock date must be after the open date."));
        }

        return Result.Success(new InvestmentDeclarationWindow(
            InvestmentDeclarationWindowId.New(), tenantId, financialYear.Trim(), openFrom, lockAt));
    }

    public bool IsOpenOn(DateOnly asOf) => asOf >= OpenFrom && asOf < LockAt;
}
