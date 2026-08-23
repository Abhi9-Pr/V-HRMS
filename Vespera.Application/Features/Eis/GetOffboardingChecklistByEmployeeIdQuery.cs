using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Eis;

public sealed record GetOffboardingChecklistByEmployeeIdQuery(Guid EmployeeId) : IRequest<Result<OffboardingChecklistDto>>;

public sealed record OffboardingChecklistDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly ExitDate,
    string AccessRevokedStatus,
    DateTimeOffset? AccessRevokedAt,
    string AssetsRecoveredStatus,
    DateTimeOffset? AssetsRecoveredAt,
    string FinalSettlementStatus,
    DateTimeOffset? FinalSettlementAt);
