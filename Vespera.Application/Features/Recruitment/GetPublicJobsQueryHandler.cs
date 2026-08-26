using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

/// <summary>
/// Backs the anonymous public careers page. Tenant identity still comes from
/// <see cref="ITenantContext"/> — <c>HttpTenantContext</c> already resolves it from the pre-auth
/// <c>X-Tenant-Id</c> header when there is no JWT (the same mechanism Login/ForgotPassword rely
/// on), so this query needs no special-casing and still runs behind <c>TenantScopeBehavior</c>
/// like every other request: an anonymous caller who omits or gets the tenant header wrong sees
/// an empty list, never another tenant's postings. A genuine cross-aggregate read (JobRequisition
/// joined to Department for a display name), so it goes through <see cref="IVesperaDbContext"/>
/// directly per CONTRIBUTING-slices.md's guidance, rather than IReadRepository+ISpecification.
/// </summary>
public sealed class GetPublicJobsQueryHandler : IRequestHandler<GetPublicJobsQuery, Result<IReadOnlyList<PublicJobDto>>>
{
    private readonly IVesperaDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetPublicJobsQueryHandler(IVesperaDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public Task<Result<IReadOnlyList<PublicJobDto>>> Handle(GetPublicJobsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var jobs = (
            from requisition in _dbContext.Set<JobRequisition>()
            where requisition.TenantId == tenantId && requisition.IsDeleted == false
                && requisition.ApprovalStatus == RequisitionApprovalStatus.Approved
                && requisition.IsPublished && requisition.Status == JobRequisitionStatus.Open
            join department in _dbContext.Set<Department>() on requisition.DepartmentId equals department.Id
            select new PublicJobDto(requisition.Id.Value, requisition.Title, department.Name, requisition.OpeningsCount))
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<PublicJobDto>>(jobs));
    }
}
