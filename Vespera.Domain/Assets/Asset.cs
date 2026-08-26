using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Assets;

public readonly record struct AssetId(Guid Value)
{
    public static AssetId New() => new(Guid.NewGuid());
}

public enum AssetStatus
{
    InStock,
    Assigned,
    UnderRepair,
    Retired,
}

public sealed class Asset : AuditableTenantAggregateRoot<AssetId>
{
    private Asset(
        AssetId id, TenantId tenantId, string assetTag, string category, Money purchaseCost, DateOnly purchaseDate,
        string? serialNumber, string? macAddress, DateOnly? warrantyExpiryDate,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        AssetTag = assetTag;
        Category = category;
        PurchaseCost = purchaseCost;
        PurchaseDate = purchaseDate;
        SerialNumber = serialNumber;
        MacAddress = macAddress;
        WarrantyExpiryDate = warrantyExpiryDate;
        Status = AssetStatus.InStock;
    }

    public string AssetTag { get; }

    public string Category { get; private set; }

    public Money PurchaseCost { get; }

    public DateOnly PurchaseDate { get; }

    public string? SerialNumber { get; private set; }

    public string? MacAddress { get; private set; }

    public DateOnly? WarrantyExpiryDate { get; private set; }

    public DepreciationSchedule? Depreciation { get; private set; }

    public AssetStatus Status { get; private set; }

    public static Result<Asset> Create(
        TenantId tenantId, string assetTag, string category, Money purchaseCost, DateOnly purchaseDate,
        DateTimeOffset occurredOn, string createdBy,
        string? serialNumber = null, string? macAddress = null, DateOnly? warrantyExpiryDate = null)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
        {
            return Result.Failure<Asset>(Error.Validation("asset.tag_required", "Asset tag is required."));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return Result.Failure<Asset>(Error.Validation("asset.category_required", "Category is required."));
        }

        return Result.Success(new Asset(
            AssetId.New(), tenantId, assetTag.Trim(), category.Trim(), purchaseCost, purchaseDate,
            serialNumber?.Trim(), macAddress?.Trim(), warrantyExpiryDate, occurredOn, createdBy));
    }

    public Result ConfigureDepreciation(
        DepreciationMethod method, int usefulLifeMonths, Money salvageValue, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (salvageValue.Currency != PurchaseCost.Currency)
        {
            return Result.Failure(Error.Validation(
                "asset.depreciation_currency_mismatch", "Salvage value must be in the same currency as the purchase cost."));
        }

        if (salvageValue.Amount > PurchaseCost.Amount)
        {
            return Result.Failure(Error.Validation(
                "asset.salvage_exceeds_cost", "Salvage value cannot exceed the purchase cost."));
        }

        var scheduleResult = DepreciationSchedule.Create(method, usefulLifeMonths, salvageValue);
        if (scheduleResult.IsFailure)
        {
            return Result.Failure(scheduleResult.Error);
        }

        Depreciation = scheduleResult.Value;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result<Money> BookValueAsOf(DateOnly asOf)
    {
        if (Depreciation is null)
        {
            return Result.Failure<Money>(
                Error.Conflict("asset.depreciation_not_configured", "This asset has no depreciation schedule configured."));
        }

        var totalMonthsElapsed = ((asOf.Year - PurchaseDate.Year) * 12) + asOf.Month - PurchaseDate.Month;
        var monthsElapsed = Math.Clamp(totalMonthsElapsed, 0, Depreciation.UsefulLifeMonths);

        var cost = PurchaseCost.Amount;
        var salvage = Depreciation.SalvageValue.Amount;
        var usefulLife = Depreciation.UsefulLifeMonths;

        decimal bookValueAmount;

        if (Depreciation.Method == DepreciationMethod.StraightLine || salvage <= 0)
        {
            var depreciableBase = cost - salvage;
            var depreciated = monthsElapsed == usefulLife ? depreciableBase : depreciableBase * monthsElapsed / usefulLife;
            bookValueAmount = cost - depreciated;
        }
        else
        {
            var rate = 1.0 - Math.Pow((double)(salvage / cost), 1.0 / usefulLife);
            bookValueAmount = (decimal)((double)cost * Math.Pow(1.0 - rate, monthsElapsed));
        }

        return Result.Success(Money.Of(Math.Round(bookValueAmount, 2), PurchaseCost.Currency));
    }

    public Result MarkAssigned(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status != AssetStatus.InStock)
        {
            return Result.Failure(Error.Conflict("asset.not_in_stock", "Only an in-stock asset can be assigned."));
        }

        Status = AssetStatus.Assigned;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result ReturnToStock(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status != AssetStatus.Assigned)
        {
            return Result.Failure(Error.Conflict("asset.not_assigned", "Only an assigned asset can be returned to stock."));
        }

        Status = AssetStatus.InStock;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result MarkUnderRepair(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == AssetStatus.Retired)
        {
            return Result.Failure(Error.Conflict("asset.retired", "A retired asset cannot be marked under repair."));
        }

        Status = AssetStatus.UnderRepair;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Retire(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == AssetStatus.Retired)
        {
            return Result.Failure(Error.Conflict("asset.already_retired", "Asset is already retired."));
        }

        Status = AssetStatus.Retired;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
