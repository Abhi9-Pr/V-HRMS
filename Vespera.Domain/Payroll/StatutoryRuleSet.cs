using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct StatutoryRuleSetId(Guid Value)
{
    public static StatutoryRuleSetId New() => new(Guid.NewGuid());
}

public enum StatutoryRuleType
{
    ProvidentFund,
    EmployeeStateInsurance,
    ProfessionalTax,
    Gratuity,
}

public sealed class StatutoryRuleSet : EffectiveDated<StatutoryRuleSetId>, ITenantScoped
{
    private StatutoryRuleSet(
        StatutoryRuleSetId id, TenantId tenantId, StatutoryRuleType ruleType, decimal ratePercent, Money? capAmount,
        DateOnly validFrom, DateOnly? validTo)
        : base(id, validFrom, validTo)
    {
        TenantId = tenantId;
        RuleType = ruleType;
        RatePercent = ratePercent;
        CapAmount = capAmount;
    }

    public TenantId TenantId { get; }

    public StatutoryRuleType RuleType { get; }

    public decimal RatePercent { get; }

    public Money? CapAmount { get; }

    public static Result<StatutoryRuleSet> Create(
        TenantId tenantId, StatutoryRuleType ruleType, decimal ratePercent, Money? capAmount, DateOnly validFrom, DateOnly? validTo)
    {
        if (ratePercent is < 0 or > 100)
        {
            return Result.Failure<StatutoryRuleSet>(Error.Validation("statutory_rule_set.invalid_rate", "Rate percent must be between 0 and 100."));
        }

        return Result.Success(new StatutoryRuleSet(StatutoryRuleSetId.New(), tenantId, ruleType, ratePercent, capAmount, validFrom, validTo));
    }

    public Result EndOn(DateOnly validTo) => Close(validTo);
}
