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
}
