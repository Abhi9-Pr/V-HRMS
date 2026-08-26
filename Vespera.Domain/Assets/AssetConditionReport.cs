namespace Vespera.Domain.Assets;

public enum AssetConditionRating
{
    Excellent,
    Good,
    Fair,
    Poor,
    Damaged,
}

public sealed record AssetConditionReport(AssetConditionRating Rating, string? Notes, DateTimeOffset RecordedAt, string RecordedBy);
