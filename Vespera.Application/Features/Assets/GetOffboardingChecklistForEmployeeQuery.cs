using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record GetOffboardingChecklistForEmployeeQuery(Guid EmployeeId) : IRequest<Result<OffboardingChecklistDto>>;

public sealed record OffboardingChecklistItemDto(string Description, bool IsComplete);

public sealed record OffboardingChecklistDto(Guid Id, Guid EmployeeId, bool IsComplete, IReadOnlyList<OffboardingChecklistItemDto> Items);
