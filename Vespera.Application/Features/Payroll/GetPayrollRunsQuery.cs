using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record PayrollRunSummaryDto(Guid Id, int Month, int Year, string Status, int EmployeeCount);

public sealed record GetPayrollRunsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<PayrollRunSummaryDto>>>;
