using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record InvestmentDeclarationQueueItemDto(Guid Id, Guid EmployeeId, string FinancialYear, int LineCount, int PendingLineCount);

public sealed record GetInvestmentDeclarationReviewQueueQuery(PagedRequest Paging) : IRequest<Result<PagedResult<InvestmentDeclarationQueueItemDto>>>;
