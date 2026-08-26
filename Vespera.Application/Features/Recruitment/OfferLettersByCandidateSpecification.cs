using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class OfferLettersByCandidateSpecification : ISpecification<OfferLetter>
{
    public OfferLettersByCandidateSpecification(TenantId tenantId, CandidateId candidateId)
    {
        Criteria = offer => offer.TenantId == tenantId && offer.CandidateId == candidateId;
    }

    public Expression<Func<OfferLetter, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<OfferLetter, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<OfferLetter, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
