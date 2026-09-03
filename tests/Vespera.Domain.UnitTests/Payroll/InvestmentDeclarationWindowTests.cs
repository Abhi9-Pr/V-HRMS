using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Domain.UnitTests.Payroll;

public class InvestmentDeclarationWindowTests
{
    private static readonly TenantId TenantId = TenantId.New();

    [Fact]
    public void Create_Should_Succeed_For_A_Valid_Range()
    {
        var result = InvestmentDeclarationWindow.Create(TenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));

        result.IsSuccess.Should().BeTrue();
        result.Value.FinancialYear.Should().Be("2026-27");
        result.Value.OpenFrom.Should().Be(new DateOnly(2026, 4, 1));
        result.Value.LockAt.Should().Be(new DateOnly(2026, 6, 30));
    }

    [Fact]
    public void Create_Should_Fail_When_FinancialYear_Is_Blank()
    {
        var result = InvestmentDeclarationWindow.Create(TenantId, "  ", new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration_window.fy_required");
    }

    [Fact]
    public void Create_Should_Fail_When_LockAt_Is_Not_After_OpenFrom()
    {
        var result = InvestmentDeclarationWindow.Create(TenantId, "2026-27", new DateOnly(2026, 6, 30), new DateOnly(2026, 4, 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration_window.invalid_range");
    }

    [Fact]
    public void Create_Should_Fail_When_LockAt_Equals_OpenFrom()
    {
        var result = InvestmentDeclarationWindow.Create(TenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration_window.invalid_range");
    }

    [Fact]
    public void IsOpenOn_Should_Be_True_On_The_OpenFrom_Date()
    {
        var window = InvestmentDeclarationWindow.Create(TenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)).Value;

        window.IsOpenOn(new DateOnly(2026, 4, 1)).Should().BeTrue();
    }

    [Fact]
    public void IsOpenOn_Should_Be_False_On_The_LockAt_Date()
    {
        var window = InvestmentDeclarationWindow.Create(TenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)).Value;

        window.IsOpenOn(new DateOnly(2026, 6, 30)).Should().BeFalse();
    }

    [Fact]
    public void IsOpenOn_Should_Be_False_Before_OpenFrom()
    {
        var window = InvestmentDeclarationWindow.Create(TenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)).Value;

        window.IsOpenOn(new DateOnly(2026, 3, 31)).Should().BeFalse();
    }

    [Fact]
    public void New_Ids_Should_Be_Distinct()
    {
        InvestmentDeclarationWindowId.New().Should().NotBe(InvestmentDeclarationWindowId.New());
    }
}
