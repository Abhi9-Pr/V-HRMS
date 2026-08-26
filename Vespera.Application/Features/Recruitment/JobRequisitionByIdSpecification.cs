using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class JobRequisitionByIdSpecification : ISpecification<JobRequisition>
{
    public JobRequisitionByIdSpecification(JobRequisitionId requisitionId)
    {
        Criteria = requisition => requisition.Id == requisitionId;
    }

    public Expression<Func<JobRequisition, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<JobRequisition, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<JobRequisition, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
