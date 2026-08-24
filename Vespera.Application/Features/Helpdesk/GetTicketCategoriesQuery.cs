using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record GetTicketCategoriesQuery(PagedRequest Paging) : IRequest<Result<PagedResult<TicketCategoryDto>>>;

public sealed record TicketCategoryDto(Guid Id, string Name, Guid DepartmentId, Guid? DefaultSlaPolicyId);
