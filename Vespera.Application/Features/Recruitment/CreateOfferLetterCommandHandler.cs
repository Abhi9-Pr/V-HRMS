using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Recruitment;

public sealed class CreateOfferLetterCommandHandler : IRequestHandler<CreateOfferLetterCommand, Result<Guid>>
{
    private readonly IReadRepository<Candidate> _candidates;
    private readonly IWriteRepository<OfferLetter> _offerLetters;
    private readonly ITenantContext _tenantContext;

    public CreateOfferLetterCommandHandler(
        IReadRepository<Candidate> candidates, IWriteRepository<OfferLetter> offerLetters, ITenantContext tenantContext)
    {
        _candidates = candidates;
        _offerLetters = offerLetters;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateOfferLetterCommand request, CancellationToken cancellationToken)
    {
        var candidate = await _candidates.FirstOrDefaultAsync(
            new CandidateByIdSpecification(new CandidateId(request.CandidateId)), cancellationToken);

        if (candidate is null)
        {
            return Result.Failure<Guid>(Error.NotFound("candidate.not_found", "Candidate not found."));
        }

        var offer = OfferLetter.Draft(
            _tenantContext.TenantId, candidate.Id, new DesignationId(request.ProposedDesignationId),
            Money.Of(request.ProposedCtc, request.Currency), request.JoiningDate);

        await _offerLetters.AddAsync(offer, cancellationToken);
        return Result.Success(offer.Id.Value);
    }
}
