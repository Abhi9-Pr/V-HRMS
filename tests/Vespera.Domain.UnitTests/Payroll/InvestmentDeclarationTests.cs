using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class InvestmentDeclarationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 5, 1);

    [Fact]
    public void Create_Should_Fail_When_The_Window_Is_Not_Open()
    {
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        var result = InvestmentDeclaration.Create(TenantId.New(), EmployeeId.New(), TaxRegimeVersionId.New(), "2026-27", Today, window);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddLine_Should_Fail_Once_The_Window_Is_Locked()
    {
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));
        var declaration = DraftDeclaration();

        var result = declaration.AddLine("80C", Money.Of(50000m, Currency.Inr), null, Today, window);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Submit_Should_Fail_With_No_Lines()
    {
        var declaration = DraftDeclaration();
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));

        var result = declaration.Submit(Today, window);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Submit_Should_Succeed_With_At_Least_One_Line()
    {
        var declaration = DraftDeclaration();
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));
        declaration.AddLine("80C", Money.Of(50000m, Currency.Inr), "proofs/80c.pdf", Today, window);

        var result = declaration.Submit(Today, window);

        result.IsSuccess.Should().BeTrue();
        declaration.Status.Should().Be(InvestmentDeclarationStatus.Submitted);
    }

    [Fact]
    public void ReviewLine_Rejecting_Without_A_Comment_Should_Fail()
    {
        var declaration = SubmittedDeclaration();

        var result = declaration.ReviewLine(0, approved: false, comment: null, "finance@vespera.test", Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReviewLine_Should_Record_The_Reviewer_And_Comment()
    {
        var declaration = SubmittedDeclaration();

        var result = declaration.ReviewLine(0, approved: true, comment: "Verified against Form 12BB", "finance@vespera.test", Now);

        result.IsSuccess.Should().BeTrue();
        declaration.Lines[0].ReviewStatus.Should().Be(InvestmentDeclarationLineReviewStatus.Approved);
        declaration.Lines[0].ReviewedBy.Should().Be("finance@vespera.test");
    }

    [Fact]
    public void Verify_Should_Fail_While_Any_Line_Is_Still_Pending()
    {
        var declaration = SubmittedDeclaration();

        var result = declaration.Verify();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Verify_Should_Succeed_Once_Every_Line_Is_Reviewed()
    {
        var declaration = SubmittedDeclaration();
        declaration.ReviewLine(0, approved: true, "Verified", "finance@vespera.test", Now);

        var result = declaration.Verify();

        result.IsSuccess.Should().BeTrue();
        declaration.Status.Should().Be(InvestmentDeclarationStatus.Verified);
    }

    [Fact]
    public void ApprovedExemptionTotal_Should_Only_Sum_Approved_Lines()
    {
        var declaration = SubmittedDeclaration();
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));
        declaration.ReviewLine(0, approved: true, "Verified", "finance@vespera.test", Now);

        declaration.ApprovedExemptionTotal(Currency.Inr).Should().Be(Money.Of(50000m, Currency.Inr));
    }

    [Fact]
    public void InvestmentDeclarationLine_Equality_Should_Compare_By_Value()
    {
        var first = SubmittedDeclaration();
        var second = SubmittedDeclaration();

        first.Lines[0].Should().Be(second.Lines[0]);
    }

    [Fact]
    public void Create_Should_Fail_When_FinancialYear_Is_Blank()
    {
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));

        var result = InvestmentDeclaration.Create(TenantId.New(), EmployeeId.New(), TaxRegimeVersionId.New(), "  ", Today, window);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.fy_required");
    }

    [Fact]
    public void AddLine_Should_Fail_Once_The_Declaration_Is_No_Longer_Draft()
    {
        var declaration = SubmittedDeclaration();
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));

        var result = declaration.AddLine("80C", Money.Of(1000m, Currency.Inr), null, Today, window);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.not_draft");
    }

    [Fact]
    public void Submit_Should_Fail_Once_Already_Submitted()
    {
        var declaration = SubmittedDeclaration();
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));

        var result = declaration.Submit(Today, window);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.not_draft");
    }

    [Fact]
    public void ReviewLine_Should_Fail_When_The_Declaration_Is_Still_A_Draft()
    {
        var declaration = DraftDeclaration();

        var result = declaration.ReviewLine(0, approved: true, "ok", "finance@vespera.test", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.not_submitted");
    }

    [Fact]
    public void ReviewLine_Should_Fail_For_An_OutOfRange_Index()
    {
        var declaration = SubmittedDeclaration();

        var result = declaration.ReviewLine(5, approved: true, "ok", "finance@vespera.test", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.invalid_line");
    }

    [Fact]
    public void Verify_Should_Fail_While_Still_A_Draft()
    {
        var declaration = DraftDeclaration();

        var result = declaration.Verify();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.not_submitted");
    }

    private static InvestmentDeclarationWindow OpenWindow(DateOnly openFrom, DateOnly lockAt) =>
        InvestmentDeclarationWindow.Create(TenantId.New(), "2026-27", openFrom, lockAt).Value;

    private static InvestmentDeclaration DraftDeclaration()
    {
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));
        return InvestmentDeclaration.Create(TenantId.New(), EmployeeId.New(), TaxRegimeVersionId.New(), "2026-27", Today, window).Value;
    }

    private static InvestmentDeclaration SubmittedDeclaration()
    {
        var window = OpenWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));
        var declaration = DraftDeclaration();
        declaration.AddLine("80C", Money.Of(50000m, Currency.Inr), "proofs/80c.pdf", Today, window);
        declaration.Submit(Today, window);
        return declaration;
    }
}
