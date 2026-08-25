using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class OfferLetterByIdSpecification : ISpecification<OfferLetter>
{
    public OfferLetterByIdSpecification(OfferLetterId offerLetterId)
    {
        Criteria = offer => offer.Id == offerLetterId;
    }

    public Expression<Func<OfferLetter, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<OfferLetter, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<OfferLetter, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
