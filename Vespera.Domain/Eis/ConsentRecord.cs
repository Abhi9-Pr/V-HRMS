using Vespera.Domain.Common;

namespace Vespera.Domain.Eis;

public readonly record struct ConsentRecordId(Guid Value)
{
    public static ConsentRecordId New() => new(Guid.NewGuid());
}

public enum ConsentType
{
    DataProcessing,
    BackgroundCheck,
    PhotoUsage,
}

public sealed class ConsentRecord : Entity<ConsentRecordId>
{
    internal ConsentRecord(ConsentRecordId id, ConsentType consentType, DateTimeOffset grantedAt)
        : base(id)
    {
        ConsentType = consentType;
        Granted = true;
        GrantedAt = grantedAt;
    }

    public ConsentType ConsentType { get; }

    public bool Granted { get; private set; }

    public DateTimeOffset GrantedAt { get; }

    public DateTimeOffset? WithdrawnAt { get; private set; }

    public Result Withdraw(DateTimeOffset occurredOn)
    {
        if (!Granted)
        {
            return Result.Failure(Error.Conflict("consent_record.already_withdrawn", "Consent is already withdrawn."));
        }

        Granted = false;
        WithdrawnAt = occurredOn;
        return Result.Success();
    }

    public Result Grant()
    {
        if (Granted)
        {
            return Result.Failure(Error.Conflict("consent_record.already_granted", "Consent is already granted."));
        }

        Granted = true;
        WithdrawnAt = null;
        return Result.Success();
    }
}
