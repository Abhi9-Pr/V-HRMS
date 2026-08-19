using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public sealed class InvestmentDeclarationLine : ValueObject
{
    private InvestmentDeclarationLine(string section, Money amount)
    {
        Section = section;
        Amount = amount;
    }

    public string Section { get; }

    public Money Amount { get; }

    public static InvestmentDeclarationLine Of(string section, Money amount) => new(section, amount);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Section;
        yield return Amount;
    }
}

public readonly record struct InvestmentDeclarationId(Guid Value)
{
    public static InvestmentDeclarationId New() => new(Guid.NewGuid());
}

public enum InvestmentDeclarationStatus
{
    Draft,
    Submitted,
    Verified,
}

public sealed class InvestmentDeclaration : AggregateRoot<InvestmentDeclarationId>, ITenantScoped
{
    private readonly List<InvestmentDeclarationLine> _lines = [];

    private InvestmentDeclaration(
        InvestmentDeclarationId id, TenantId tenantId, EmployeeId employeeId, TaxRegimeVersionId taxRegimeVersionId, string financialYear)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        TaxRegimeVersionId = taxRegimeVersionId;
        FinancialYear = financialYear;
        Status = InvestmentDeclarationStatus.Draft;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public TaxRegimeVersionId TaxRegimeVersionId { get; }

    public string FinancialYear { get; }

    public InvestmentDeclarationStatus Status { get; private set; }

    public IReadOnlyList<InvestmentDeclarationLine> Lines => _lines.AsReadOnly();

    public static Result<InvestmentDeclaration> Create(
        TenantId tenantId, EmployeeId employeeId, TaxRegimeVersionId taxRegimeVersionId, string financialYear)
    {
        if (string.IsNullOrWhiteSpace(financialYear))
        {
            return Result.Failure<InvestmentDeclaration>(
                Error.Validation("investment_declaration.fy_required", "Financial year is required."));
        }

        return Result.Success(new InvestmentDeclaration(
            InvestmentDeclarationId.New(), tenantId, employeeId, taxRegimeVersionId, financialYear.Trim()));
    }

    public Result AddLine(string section, Money amount)
    {
        if (Status != InvestmentDeclarationStatus.Draft)
        {
            return Result.Failure(Error.Conflict("investment_declaration.not_draft", "Lines can only be added while the declaration is in Draft."));
        }

        _lines.Add(InvestmentDeclarationLine.Of(section, amount));
        return Result.Success();
    }

    public Result Submit()
    {
        if (Status != InvestmentDeclarationStatus.Draft)
        {
            return Result.Failure(Error.Conflict("investment_declaration.not_draft", "Only a draft declaration can be submitted."));
        }

        Status = InvestmentDeclarationStatus.Submitted;
        return Result.Success();
    }

    public Result Verify()
    {
        if (Status != InvestmentDeclarationStatus.Submitted)
        {
            return Result.Failure(Error.Conflict("investment_declaration.not_submitted", "Only a submitted declaration can be verified."));
        }

        Status = InvestmentDeclarationStatus.Verified;
        return Result.Success();
    }
}
