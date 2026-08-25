using MediatR;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

public sealed record GetAssetByIdQuery(Guid Id) : IRequest<Result<AssetDetailDto>>;

public sealed record AssetAssignmentSummaryDto(
    Guid Id, Guid EmployeeId, DateTimeOffset AssignedAt, DateTimeOffset? ReturnedAt, string? ReturnCondition);

public sealed record AssetDetailDto(
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
    DepreciationScheduleDto? Depreciation,
    decimal? BookValue,
    Currency? BookValueCurrency,
    IReadOnlyList<AssetAssignmentSummaryDto> Assignments);
