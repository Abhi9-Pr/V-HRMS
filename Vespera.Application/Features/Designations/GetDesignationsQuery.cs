using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Designations;

public sealed record GetDesignationsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<DesignationDto>>>;

public sealed record DesignationDto(Guid Id, string Title, int Grade);

/// <summary>Compact profile for mobile list responses — ID plus display-critical fields only.</summary>
public sealed record DesignationSummaryDto(Guid Id, string Title);
