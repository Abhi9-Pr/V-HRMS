using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

public sealed record GetAssetsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<AssetDto>>>;

public sealed record DepreciationScheduleDto(DepreciationMethod Method, int UsefulLifeMonths, decimal SalvageValue, Currency SalvageValueCurrency);

public sealed record AssetDto(
    Guid Id,
    string AssetTag,
    string Category,
    decimal PurchaseCost,
    Currency PurchaseCostCurrency,
    DateOnly PurchaseDate,
    string? SerialNumber,
    string? MacAddress,
    DateOnly? WarrantyExpiryDate,
    AssetStatus Status,
    DepreciationScheduleDto? Depreciation);
