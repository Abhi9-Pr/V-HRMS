using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public enum InvestmentDeclarationLineReviewStatus
{
    Pending,
    Approved,
    Rejected,
}

public sealed class InvestmentDeclarationLine : ValueObject
{
    private InvestmentDeclarationLine(
        string section, Money amount, string? proofFileReference, InvestmentDeclarationLineReviewStatus reviewStatus,
        string? reviewComment, string? reviewedBy, DateTimeOffset? reviewedAt)
    {
        Section = section;
        Amount = amount;
        ProofFileReference = proofFileReference;
        ReviewStatus = reviewStatus;
        ReviewComment = reviewComment;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
    }

    public string Section { get; }

    public Money Amount { get; }

    public string? ProofFileReference { get; }

    public InvestmentDeclarationLineReviewStatus ReviewStatus { get; }

    public string? ReviewComment { get; }

    public string? ReviewedBy { get; }

    public DateTimeOffset? ReviewedAt { get; }

    public static InvestmentDeclarationLine Of(string section, Money amount, string? proofFileReference) =>
        new(section, amount, proofFileReference, InvestmentDeclarationLineReviewStatus.Pending, null, null, null);

    /// <summary>Lines are value objects, so reviewing one produces a new instance — the caller
    /// (<see cref="InvestmentDeclaration.ReviewLine"/>) replaces the list entry at its index.</summary>
    internal InvestmentDeclarationLine Reviewed(bool approved, string? comment, string reviewedBy, DateTimeOffset occurredOn) =>
        new(
            Section, Amount, ProofFileReference,
            approved ? InvestmentDeclarationLineReviewStatus.Approved : InvestmentDeclarationLineReviewStatus.Rejected,
            comment, reviewedBy, occurredOn);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Section;
        yield return Amount;
        yield return ProofFileReference;
        yield return ReviewStatus;
        yield return ReviewComment;
        yield return ReviewedBy;
        yield return ReviewedAt;
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

/// <summary>
/// An employee's investment declaration for one financial year, under a chosen tax regime.
/// Finance reviews it line by line (<see cref="ReviewLine"/>, each with its own comment) rather
/// than approving or rejecting the whole thing at once — <see cref="Verify"/> requires every line
/// to have left <see cref="InvestmentDeclarationLineReviewStatus.Pending"/> first. Only approved
/// lines feed the tax rule, via <see cref="ApprovedExemptionTotal"/>. Creating, adding a line, or
/// submitting are all checked against the tenant's <see cref="InvestmentDeclarationWindow"/> for
/// the declared financial year.
/// </summary>
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
        TenantId tenantId, EmployeeId employeeId, TaxRegimeVersionId taxRegimeVersionId, string financialYear,
        DateOnly asOf, InvestmentDeclarationWindow window)
    {
        if (string.IsNullOrWhiteSpace(financialYear))
        {
            return Result.Failure<InvestmentDeclaration>(
                Error.Validation("investment_declaration.fy_required", "Financial year is required."));
        }

        if (!window.IsOpenOn(asOf))
        {
            return Result.Failure<InvestmentDeclaration>(
                Error.Conflict("investment_declaration.window_closed", "The investment declaration window is not open."));
        }

        return Result.Success(new InvestmentDeclaration(
            InvestmentDeclarationId.New(), tenantId, employeeId, taxRegimeVersionId, financialYear.Trim()));
    }

    public Result AddLine(string section, Money amount, string? proofFileReference, DateOnly asOf, InvestmentDeclarationWindow window)
    {
        if (Status != InvestmentDeclarationStatus.Draft)
        {
            return Result.Failure(Error.Conflict("investment_declaration.not_draft", "Lines can only be added while the declaration is in Draft."));
        }

        if (!window.IsOpenOn(asOf))
        {
            return Result.Failure(Error.Conflict("investment_declaration.window_closed", "The investment declaration window is not open."));
        }

        _lines.Add(InvestmentDeclarationLine.Of(section, amount, proofFileReference));
        return Result.Success();
    }

    public Result Submit(DateOnly asOf, InvestmentDeclarationWindow window)
    {
        if (Status != InvestmentDeclarationStatus.Draft)
        {
            return Result.Failure(Error.Conflict("investment_declaration.not_draft", "Only a draft declaration can be submitted."));
        }

        if (_lines.Count == 0)
        {
            return Result.Failure(Error.Validation("investment_declaration.empty", "Cannot submit a declaration with no lines."));
        }

        if (!window.IsOpenOn(asOf))
        {
            return Result.Failure(Error.Conflict("investment_declaration.window_closed", "The investment declaration window is not open."));
        }

        Status = InvestmentDeclarationStatus.Submitted;
        return Result.Success();
    }

    /// <summary>Finance reviews one line at a time, each with its own approve/reject and comment —
    /// a rejection requires a comment explaining why.</summary>
    public Result ReviewLine(int lineIndex, bool approved, string? comment, string reviewedBy, DateTimeOffset occurredOn)
    {
        if (Status != InvestmentDeclarationStatus.Submitted)
        {
            return Result.Failure(Error.Conflict("investment_declaration.not_submitted", "Only a submitted declaration's lines can be reviewed."));
        }

        if (lineIndex < 0 || lineIndex >= _lines.Count)
        {
            return Result.Failure(Error.Validation("investment_declaration.invalid_line", "No such declaration line."));
        }

        if (!approved && string.IsNullOrWhiteSpace(comment))
        {
            return Result.Failure(Error.Validation("investment_declaration.rejection_comment_required", "Rejecting a line requires a comment."));
        }

        _lines[lineIndex] = _lines[lineIndex].Reviewed(approved, comment?.Trim(), reviewedBy, occurredOn);
        return Result.Success();
    }

    public Result Verify()
    {
        if (Status != InvestmentDeclarationStatus.Submitted)
        {
            return Result.Failure(Error.Conflict("investment_declaration.not_submitted", "Only a submitted declaration can be verified."));
        }

        if (_lines.Any(line => line.ReviewStatus == InvestmentDeclarationLineReviewStatus.Pending))
        {
            return Result.Failure(Error.Conflict(
                "investment_declaration.lines_pending_review", "Every line must be reviewed before the declaration can be verified."));
        }

        Status = InvestmentDeclarationStatus.Verified;
        return Result.Success();
    }

    /// <summary>What <c>IncomeTaxRule</c> reads: the sum of every finance-approved line, ignoring
    /// pending and rejected ones. Meaningful once <see cref="Status"/> is <see cref="InvestmentDeclarationStatus.Verified"/>.</summary>
    public Money ApprovedExemptionTotal(Currency currency) =>
        _lines.Where(line => line.ReviewStatus == InvestmentDeclarationLineReviewStatus.Approved)
            .Aggregate(Money.Zero(currency), (total, line) => total + line.Amount);
}
