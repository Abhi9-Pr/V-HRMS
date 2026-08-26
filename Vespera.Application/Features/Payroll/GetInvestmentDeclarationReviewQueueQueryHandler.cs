using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class GetInvestmentDeclarationReviewQueueQueryHandler
    : IRequestHandler<GetInvestmentDeclarationReviewQueueQuery, Result<PagedResult<InvestmentDeclarationQueueItemDto>>>
{
    private readonly IReadRepository<InvestmentDeclaration> _declarations;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public GetInvestmentDeclarationReviewQueueQueryHandler(
        IReadRepository<InvestmentDeclaration> declarations, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor)
    {
        _declarations = declarations;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<PagedResult<InvestmentDeclarationQueueItemDto>>> Handle(
        GetInvestmentDeclarationReviewQueueQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var specification = new SubmittedInvestmentDeclarationsPagedSpecification(tenantId, request.Paging);

        var declarations = await _declarations.ListAsync(specification, cancellationToken);
        var totalCount = await _declarations.CountAsync(specification, cancellationToken);

        var items = declarations
            .Select(declaration => new InvestmentDeclarationQueueItemDto(
                declaration.Id.Value, declaration.EmployeeId.Value, declaration.FinancialYear, declaration.Lines.Count,
                declaration.Lines.Count(line => line.ReviewStatus == InvestmentDeclarationLineReviewStatus.Pending)))
            .ToList();

        await _piiAccessAuditor.RecordAccessAsync(
            tenantId, "InvestmentDeclaration", Guid.Empty, "ReviewQueue", _currentUser.UserId?.ToString() ?? "system",
            _dateTimeProvider.UtcNow, cancellationToken);

        return Result.Success(new PagedResult<InvestmentDeclarationQueueItemDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
