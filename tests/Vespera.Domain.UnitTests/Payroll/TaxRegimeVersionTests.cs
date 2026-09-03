using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class TaxRegimeVersionTests
{
    [Fact]
    public void CalculateTax_Should_Apply_Progressive_Slabs()
    {
        var slabs = new[]
        {
            TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 0m).Value,
            TaxSlab.Create(Money.Of(500_000m, Currency.Inr), 5m).Value,
            TaxSlab.Create(Money.Of(1_000_000m, Currency.Inr), 20m).Value,
        };
        var regime = TaxRegimeVersion.Create(TenantId.New(), TaxRegimeType.Old, "2025-26", slabs).Value;

        var tax = regime.CalculateTax(Money.Of(600_000m, Currency.Inr));

        tax.Should().Be(Money.Of(32_500m, Currency.Inr));
    }

    [Fact]
    public void CalculateTax_Should_Be_Zero_Within_The_Zero_Rated_Slab()
    {
        var slabs = new[] { TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 0m).Value };
        var regime = TaxRegimeVersion.Create(TenantId.New(), TaxRegimeType.Old, "2025-26", slabs).Value;

        var tax = regime.CalculateTax(Money.Of(200_000m, Currency.Inr));

        tax.Should().Be(Money.Zero(Currency.Inr));
    }

    [Fact]
    public void Create_Should_Fail_With_No_Slabs()
    {
        var result = TaxRegimeVersion.Create(TenantId.New(), TaxRegimeType.New, "2025-26", []);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TaxSlab_Equality_Should_Compare_By_Value()
    {
        var first = TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 5m).Value;
        var second = TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 5m).Value;

        first.Should().Be(second);
    }

    [Fact]
    public void Create_Should_Fail_When_FinancialYear_Is_Blank()
    {
        var slabs = new[] { TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 0m).Value };

        var result = TaxRegimeVersion.Create(TenantId.New(), TaxRegimeType.New, "  ", slabs);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tax_regime_version.fy_required");
    }

    [Fact]
    public void TaxSlab_Create_Should_Fail_For_An_OutOfRange_RatePercent()
    {
        var result = TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 101m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tax_slab.invalid_rate");
    }

    [Fact]
    public void CalculateTax_Should_Stop_At_The_First_Slab_Covering_The_Full_Income()
    {
        var slabs = new[]
        {
            TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 0m).Value,
            TaxSlab.Create(Money.Of(500_000m, Currency.Inr), 5m).Value,
            TaxSlab.Create(Money.Of(1_000_000m, Currency.Inr), 20m).Value,
        };
        var regime = TaxRegimeVersion.Create(TenantId.New(), TaxRegimeType.Old, "2025-26", slabs).Value;

        var tax = regime.CalculateTax(Money.Of(100_000m, Currency.Inr));

        tax.Should().Be(Money.Zero(Currency.Inr));
    }

    [Fact]
    public void Slabs_Should_Be_Ordered_By_UpTo_Regardless_Of_Insertion_Order()
    {
        var slabs = new[]
        {
            TaxSlab.Create(Money.Of(1_000_000m, Currency.Inr), 20m).Value,
            TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 0m).Value,
            TaxSlab.Create(Money.Of(500_000m, Currency.Inr), 5m).Value,
        };
        var regime = TaxRegimeVersion.Create(TenantId.New(), TaxRegimeType.Old, "2025-26", slabs).Value;

        regime.Slabs.Select(s => s.UpTo.Amount).Should().Equal(250_000m, 500_000m, 1_000_000m);
    }
}
