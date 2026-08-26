using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record GetOffboardingChecklistForEmployeeQuery(Guid EmployeeId) : IRequest<Result<AssetOffboardingChecklistDto>>;

public sealed record AssetOffboardingChecklistItemDto(string Description, bool IsComplete);

// Named with the "Asset" prefix (not just OffboardingChecklistDto) because
// Vespera.Application.Features.Eis also has its own OffboardingChecklistDto (a different,
// higher-level employee-exit checklist) — Swashbuckle's default schema-id strategy is the bare
// class name, so two same-named DTOs in different namespaces collide when both are exposed via
// [ProducesResponseType].
public sealed record AssetOffboardingChecklistDto(
    Guid Id, Guid EmployeeId, bool IsComplete, IReadOnlyList<AssetOffboardingChecklistItemDto> Items);
