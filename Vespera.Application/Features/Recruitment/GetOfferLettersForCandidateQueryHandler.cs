using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetOfferLettersForCandidateQueryHandler
    : IRequestHandler<GetOfferLettersForCandidateQuery, Result<IReadOnlyList<OfferLetterDto>>>
{
    private readonly IReadRepository<OfferLetter> _offerLetters;
    private readonly ITenantContext _tenantContext;

    public GetOfferLettersForCandidateQueryHandler(IReadRepository<OfferLetter> offerLetters, ITenantContext tenantContext)
    {
        _offerLetters = offerLetters;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<OfferLetterDto>>> Handle(
        GetOfferLettersForCandidateQuery request, CancellationToken cancellationToken)
    {
        var offers = await _offerLetters.ListAsync(
            new OfferLettersByCandidateSpecification(_tenantContext.TenantId, new CandidateId(request.CandidateId)), cancellationToken);

        return Result.Success<IReadOnlyList<OfferLetterDto>>(offers.Adapt<List<OfferLetterDto>>());
    }
}
