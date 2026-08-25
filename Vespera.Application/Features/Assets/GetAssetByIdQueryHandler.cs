using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class GetAssetByIdQueryHandler : IRequestHandler<GetAssetByIdQuery, Result<AssetDetailDto>>
{
    private readonly IReadRepository<Asset> _assets;
    private readonly IReadRepository<AssetAssignment> _assignments;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAssetByIdQueryHandler(
        IReadRepository<Asset> assets, IReadRepository<AssetAssignment> assignments, IDateTimeProvider dateTimeProvider)
    {
        _assets = assets;
        _assignments = assignments;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AssetDetailDto>> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var asset = await _assets.FirstOrDefaultAsync(new AssetByIdSpecification(new AssetId(request.Id)), cancellationToken);
        if (asset is null)
        {
            return Result.Failure<AssetDetailDto>(Error.NotFound("asset.not_found", "Asset not found."));
        }

        var assignments = await _assignments.ListAsync(new AssetAssignmentsByAssetSpecification(asset.Id), cancellationToken);

        var bookValueResult = asset.BookValueAsOf(DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime));

        var dto = new AssetDetailDto(
            asset.Id.Value,
            asset.AssetTag,
            asset.Category,
            asset.PurchaseCost.Amount,
            asset.PurchaseCost.Currency,
            asset.PurchaseDate,
            asset.SerialNumber,
            asset.MacAddress,
            asset.WarrantyExpiryDate,
            asset.Status,
            asset.Depreciation is null
                ? null
                : new DepreciationScheduleDto(
                    asset.Depreciation.Method, asset.Depreciation.UsefulLifeMonths,
                    asset.Depreciation.SalvageValue.Amount, asset.Depreciation.SalvageValue.Currency),
            bookValueResult.IsSuccess ? bookValueResult.Value.Amount : null,
            bookValueResult.IsSuccess ? bookValueResult.Value.Currency : null,
            assignments.Adapt<List<AssetAssignmentSummaryDto>>());

        return Result.Success(dto);
    }
}
