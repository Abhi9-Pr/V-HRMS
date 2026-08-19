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
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        AssetTag = assetTag;
        Category = category;
        PurchaseCost = purchaseCost;
        PurchaseDate = purchaseDate;
        Status = AssetStatus.InStock;
    }

    public string AssetTag { get; }

    public string Category { get; private set; }

    public Money PurchaseCost { get; }

    public DateOnly PurchaseDate { get; }

    public AssetStatus Status { get; private set; }

    public static Result<Asset> Create(
        TenantId tenantId, string assetTag, string category, Money purchaseCost, DateOnly purchaseDate,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
        {
            return Result.Failure<Asset>(Error.Validation("asset.tag_required", "Asset tag is required."));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return Result.Failure<Asset>(Error.Validation("asset.category_required", "Category is required."));
        }

        return Result.Success(new Asset(AssetId.New(), tenantId, assetTag.Trim(), category.Trim(), purchaseCost, purchaseDate, occurredOn, createdBy));
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
