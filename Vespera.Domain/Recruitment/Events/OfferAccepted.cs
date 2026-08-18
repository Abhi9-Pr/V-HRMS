using Vespera.Domain.Common;

namespace Vespera.Domain.Recruitment.Events;

public sealed record OfferAccepted(OfferLetterId OfferLetterId, CandidateId CandidateId, DateOnly JoiningDate, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
