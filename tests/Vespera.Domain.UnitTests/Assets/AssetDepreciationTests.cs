using FluentAssertions;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Assets;

public class AssetDepreciationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PurchaseDate = new(2026, 1, 1);

    private static Asset CreateAsset(decimal cost = 120000m) =>
        Asset.Create(TenantId.New(), "AST-001", "Laptop", Money.Of(cost, Currency.Inr), PurchaseDate, Now, "admin@vespera.test").Value;

    [Fact]
    public void ConfigureDepreciation_Should_Fail_When_Currency_Mismatches()
    {
        var asset = CreateAsset();

        var result = asset.ConfigureDepreciation(
            DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Usd), Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ConfigureDepreciation_Should_Fail_When_Salvage_Exceeds_Cost()
    {
        var asset = CreateAsset(cost: 120000m);

        var result = asset.ConfigureDepreciation(
            DepreciationMethod.StraightLine, 24, Money.Of(150000m, Currency.Inr), Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ConfigureDepreciation_Should_Succeed_And_Store_Schedule()
    {
        var asset = CreateAsset();

        var result = asset.ConfigureDepreciation(
            DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        asset.Depreciation.Should().NotBeNull();
        asset.Depreciation!.Method.Should().Be(DepreciationMethod.StraightLine);
        asset.Depreciation.UsefulLifeMonths.Should().Be(24);
    }

    [Fact]
    public void BookValueAsOf_Should_Fail_When_Depreciation_Not_Configured()
    {
        var asset = CreateAsset();

        var result = asset.BookValueAsOf(PurchaseDate);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void StraightLine_Should_Compute_Linear_Depreciation_At_The_Midpoint()
    {
        var asset = CreateAsset(cost: 120000m);
        asset.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), Now, "admin@vespera.test");

        var result = asset.BookValueAsOf(new DateOnly(2027, 1, 1)); // 12 months elapsed of 24

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Money.Of(70000m, Currency.Inr));
    }

    [Fact]
    public void StraightLine_Should_Floor_At_SalvageValue_Once_UsefulLife_Has_Elapsed()
    {
        var asset = CreateAsset(cost: 120000m);
        asset.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), Now, "admin@vespera.test");

        var result = asset.BookValueAsOf(new DateOnly(2029, 1, 1)); // well past the 24-month useful life

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Money.Of(20000m, Currency.Inr));
    }

    [Fact]
    public void DecliningBalance_Should_Return_PurchaseCost_At_Zero_Months_Elapsed()
    {
        var asset = CreateAsset(cost: 120000m);
        asset.ConfigureDepreciation(DepreciationMethod.DecliningBalance, 24, Money.Of(20000m, Currency.Inr), Now, "admin@vespera.test");

        var result = asset.BookValueAsOf(PurchaseDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Money.Of(120000m, Currency.Inr));
    }

    [Fact]
    public void DecliningBalance_Should_Return_SalvageValue_Once_UsefulLife_Has_Elapsed()
    {
        var asset = CreateAsset(cost: 120000m);
        asset.ConfigureDepreciation(DepreciationMethod.DecliningBalance, 24, Money.Of(20000m, Currency.Inr), Now, "admin@vespera.test");

        var result = asset.BookValueAsOf(new DateOnly(2028, 1, 1)); // exactly 24 months elapsed

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().BeApproximately(20000m, 0.01m);
        result.Value.Currency.Should().Be(Currency.Inr);
    }

    [Fact]
    public void Schedules_With_The_Same_Method_Life_And_Salvage_Should_Be_Equal()
    {
        var assetA = CreateAsset();
        assetA.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), Now, "admin@vespera.test");
        var assetB = CreateAsset();
        assetB.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), Now, "admin@vespera.test");

        assetA.Depreciation.Should().Be(assetB.Depreciation);

        var assetC = CreateAsset();
        assetC.ConfigureDepreciation(DepreciationMethod.DecliningBalance, 24, Money.Of(20000m, Currency.Inr), Now, "admin@vespera.test");
        assetA.Depreciation.Should().NotBe(assetC.Depreciation);
    }
}
