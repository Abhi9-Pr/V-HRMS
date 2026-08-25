using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record GetPublicHolidaysQuery(PagedRequest Paging) : IRequest<Result<PagedResult<PublicHolidayDto>>>;

public sealed record PublicHolidayDto(Guid Id, DateOnly Date, string Name);
