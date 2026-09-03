using FluentAssertions;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Assets;

public class AssetTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Asset CreateAsset(decimal purchaseCost = 120000m) =>
        Asset.Create(TenantId, "AST-001", "Laptop", Money.Of(purchaseCost, Currency.Inr), new DateOnly(2026, 1, 1), Now, "system").Value;

    [Fact]
    public void Create_Should_Fail_When_AssetTag_Is_Blank()
    {
        var result = Asset.Create(TenantId, "   ", "Laptop", Money.Of(1000m, Currency.Inr), new DateOnly(2026, 1, 1), Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.tag_required");
    }

    [Fact]
    public void Create_Should_Fail_When_Category_Is_Blank()
    {
        var result = Asset.Create(TenantId, "AST-001", "   ", Money.Of(1000m, Currency.Inr), new DateOnly(2026, 1, 1), Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.category_required");
    }

    [Fact]
    public void Create_Should_Succeed_And_Start_InStock()
    {
        var asset = CreateAsset();

        asset.Status.Should().Be(AssetStatus.InStock);
        asset.AssetTag.Should().Be("AST-001");
        asset.Category.Should().Be("Laptop");
    }

    [Fact]
    public void ConfigureDepreciation_Should_Fail_When_Salvage_Currency_Differs_From_Purchase_Cost()
    {
        var asset = CreateAsset();

        var result = asset.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(1000m, Currency.Usd), Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.depreciation_currency_mismatch");
    }

    [Fact]
    public void ConfigureDepreciation_Should_Fail_When_Salvage_Exceeds_PurchaseCost()
    {
        var asset = CreateAsset(10000m);

        var result = asset.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.salvage_exceeds_cost");
    }

    [Fact]
    public void ConfigureDepreciation_Should_Fail_When_UsefulLifeMonths_Is_Invalid()
    {
        var asset = CreateAsset();

        var result = asset.ConfigureDepreciation(DepreciationMethod.StraightLine, 0, Money.Of(0m, Currency.Inr), Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("depreciation_schedule.invalid_useful_life");
    }

    [Fact]
    public void ConfigureDepreciation_Should_Succeed()
    {
        var asset = CreateAsset();

        var result = asset.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), Now, "system");

        result.IsSuccess.Should().BeTrue();
        asset.Depreciation.Should().NotBeNull();
    }

    [Fact]
    public void BookValueAsOf_Should_Fail_When_Depreciation_Is_Not_Configured()
    {
        var asset = CreateAsset();

        var result = asset.BookValueAsOf(new DateOnly(2027, 1, 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.depreciation_not_configured");
    }

    [Fact]
    public void BookValueAsOf_Should_Depreciate_StraightLine()
    {
        var asset = CreateAsset(120000m);
        asset.ConfigureDepreciation(DepreciationMethod.StraightLine, 24, Money.Of(20000m, Currency.Inr), Now, "system");

        var result = asset.BookValueAsOf(new DateOnly(2027, 1, 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(70000m);
    }

    [Fact]
    public void BookValueAsOf_Should_Depreciate_DecliningBalance()
    {
        var asset = CreateAsset(120000m);
        asset.ConfigureDepreciation(DepreciationMethod.DecliningBalance, 24, Money.Of(20000m, Currency.Inr), Now, "system");

        var result = asset.BookValueAsOf(new DateOnly(2027, 1, 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().BeLessThan(120000m);
    }

    [Fact]
    public void MarkAssigned_Should_Succeed_From_InStock()
    {
        var asset = CreateAsset();

        var result = asset.MarkAssigned(Now, "system");

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(AssetStatus.Assigned);
    }

    [Fact]
    public void MarkAssigned_Should_Fail_When_Not_InStock()
    {
        var asset = CreateAsset();
        asset.MarkAssigned(Now, "system");

        var result = asset.MarkAssigned(Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.not_in_stock");
    }

    [Fact]
    public void ReturnToStock_Should_Succeed_From_Assigned()
    {
        var asset = CreateAsset();
        asset.MarkAssigned(Now, "system");

        var result = asset.ReturnToStock(Now, "system");

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(AssetStatus.InStock);
    }

    [Fact]
    public void ReturnToStock_Should_Fail_When_Not_Assigned()
    {
        var asset = CreateAsset();

        var result = asset.ReturnToStock(Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.not_assigned");
    }

    [Fact]
    public void MarkUnderRepair_Should_Succeed()
    {
        var asset = CreateAsset();

        var result = asset.MarkUnderRepair(Now, "system");

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(AssetStatus.UnderRepair);
    }

    [Fact]
    public void MarkUnderRepair_Should_Fail_When_Retired()
    {
        var asset = CreateAsset();
        asset.Retire(Now, "system");

        var result = asset.MarkUnderRepair(Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.retired");
    }

    [Fact]
    public void Retire_Should_Succeed()
    {
        var asset = CreateAsset();

        var result = asset.Retire(Now, "system");

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(AssetStatus.Retired);
    }

    [Fact]
    public void Retire_Should_Fail_When_Already_Retired()
    {
        var asset = CreateAsset();
        asset.Retire(Now, "system");

        var result = asset.Retire(Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("asset.already_retired");
    }

    [Fact]
    public void AssetId_New_Should_Generate_Distinct_Values()
    {
        AssetId.New().Should().NotBe(AssetId.New());
    }
}
