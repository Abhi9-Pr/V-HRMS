using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record GetSlaPoliciesQuery(PagedRequest Paging) : IRequest<Result<PagedResult<SlaPolicyDto>>>;

public sealed record SlaPolicyDto(
    Guid Id, string Name, TimeSpan ResponseTime, TimeSpan ResolutionTime, TimeOnly BusinessHoursStart, TimeOnly BusinessHoursEnd);
