using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class CandidatesByRequisitionSpecification : ISpecification<Candidate>
{
    public CandidatesByRequisitionSpecification(TenantId tenantId, JobRequisitionId jobRequisitionId)
    {
        Criteria = candidate => candidate.TenantId == tenantId && candidate.JobRequisitionId == jobRequisitionId;
    }

    public Expression<Func<Candidate, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Candidate, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Candidate, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
