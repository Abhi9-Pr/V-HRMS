using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Recruitment;

public readonly record struct OfferLetterId(Guid Value)
{
    public static OfferLetterId New() => new(Guid.NewGuid());
}

public enum OfferLetterStatus
{
    Draft,
    Sent,
    Accepted,
    Declined,
    Withdrawn,
}

public sealed class OfferLetter : AggregateRoot<OfferLetterId>, ITenantScoped
{
    private OfferLetter(
        OfferLetterId id, TenantId tenantId, CandidateId candidateId, DesignationId proposedDesignationId,
        Money proposedCtc, DateOnly joiningDate)
        : base(id)
    {
        TenantId = tenantId;
        CandidateId = candidateId;
        ProposedDesignationId = proposedDesignationId;
        ProposedCtc = proposedCtc;
        JoiningDate = joiningDate;
        Status = OfferLetterStatus.Draft;
    }

    public TenantId TenantId { get; }

    public CandidateId CandidateId { get; }

    public DesignationId ProposedDesignationId { get; }

    public Money ProposedCtc { get; }

    public DateOnly JoiningDate { get; }

    public OfferLetterStatus Status { get; private set; }

    public static OfferLetter Draft(
        TenantId tenantId, CandidateId candidateId, DesignationId proposedDesignationId, Money proposedCtc, DateOnly joiningDate) =>
        new(OfferLetterId.New(), tenantId, candidateId, proposedDesignationId, proposedCtc, joiningDate);

    public Result Send()
    {
        if (Status != OfferLetterStatus.Draft)
        {
            return Result.Failure(Error.Conflict("offer_letter.not_draft", "Only a draft offer can be sent."));
        }

        Status = OfferLetterStatus.Sent;
        return Result.Success();
    }

    public Result Accept(DateTimeOffset occurredOn)
    {
        if (Status != OfferLetterStatus.Sent)
        {
            return Result.Failure(Error.Conflict("offer_letter.not_sent", "Only a sent offer can be accepted."));
        }

        Status = OfferLetterStatus.Accepted;
        Raise(new OfferAccepted(Id, CandidateId, JoiningDate, occurredOn));
        return Result.Success();
    }

    public Result Decline()
    {
        if (Status != OfferLetterStatus.Sent)
        {
            return Result.Failure(Error.Conflict("offer_letter.not_sent", "Only a sent offer can be declined."));
        }

        Status = OfferLetterStatus.Declined;
        return Result.Success();
    }

    public Result Withdraw()
    {
        if (Status is OfferLetterStatus.Accepted or OfferLetterStatus.Declined or OfferLetterStatus.Withdrawn)
        {
            return Result.Failure(Error.Conflict("offer_letter.cannot_withdraw", "Cannot withdraw an offer that is already resolved."));
        }

        Status = OfferLetterStatus.Withdrawn;
        return Result.Success();
    }
}
