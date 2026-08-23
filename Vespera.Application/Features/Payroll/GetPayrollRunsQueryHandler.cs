using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class GetPayrollRunsQueryHandler : IRequestHandler<GetPayrollRunsQuery, Result<PagedResult<PayrollRunSummaryDto>>>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public GetPayrollRunsQueryHandler(
        IReadRepository<PayrollRun> payrollRuns, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor)
    {
        _payrollRuns = payrollRuns;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<PagedResult<PayrollRunSummaryDto>>> Handle(GetPayrollRunsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var specification = new PayrollRunsPagedSpecification(tenantId, request.Paging);

        var payrollRuns = await _payrollRuns.ListAsync(specification, cancellationToken);
        var totalCount = await _payrollRuns.CountAsync(specification, cancellationToken);

        var items = payrollRuns
            .Select(run => new PayrollRunSummaryDto(run.Id.Value, run.Month, run.Year, run.Status.ToString(), run.Lines.Count))
            .ToList();

        await _piiAccessAuditor.RecordAccessAsync(
            tenantId, "PayrollRun", Guid.Empty, "List", _currentUser.UserId?.ToString() ?? "system", _dateTimeProvider.UtcNow, cancellationToken);

        return Result.Success(new PagedResult<PayrollRunSummaryDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
