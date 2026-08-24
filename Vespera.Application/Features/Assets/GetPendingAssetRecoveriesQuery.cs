using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

public sealed record GetPendingAssetRecoveriesQuery(PagedRequest Paging) : IRequest<Result<PagedResult<AssetRecoveryDto>>>;

public sealed record AssetRecoveryDto(
    Guid Id,
    Guid AssetAssignmentId,
    Guid AssetId,
    Guid EmployeeId,
    DateTimeOffset InitiatedAt,
    AssetRecoveryStatus Status,
    string? CourierCarrier,
    string? CourierTrackingReference,
    string? DamageAssessmentNotes,
    decimal? WriteOffAmount,
    Currency? WriteOffAmountCurrency,
    string? WriteOffReason);
