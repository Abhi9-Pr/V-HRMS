using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Attendance;

public readonly record struct AttendancePunchId(Guid Value)
{
    public static AttendancePunchId New() => new(Guid.NewGuid());
}

public enum PunchType
{
    In,
    Out,
}

public enum PunchSource
{
    Web,
    Mobile,
    Biometric,
    Kiosk,
}

public sealed class AttendancePunch : Entity<AttendancePunchId>
{
    internal AttendancePunch(
        AttendancePunchId id,
        PunchType punchType,
        DateTimeOffset punchedAtUtc,
        GeoCoordinate? location,
        PunchSource source,
        bool requiresApproval = false,
        string? flagReason = null)
        : base(id)
    {
        PunchType = punchType;
        PunchedAtUtc = punchedAtUtc;
        Location = location;
        Source = source;
        RequiresApproval = requiresApproval;
        FlagReason = flagReason;
    }

    public PunchType PunchType { get; }

    public DateTimeOffset PunchedAtUtc { get; }

    public GeoCoordinate? Location { get; }

    public PunchSource Source { get; }

    /// <summary>Set when a mobile punch tripped a soft signal (mock-provider location, implausible
    /// travel speed since the previous punch, low GPS accuracy) that's worth a manager's attention
    /// but isn't grounds to reject the punch outright. <see cref="FlagReason"/> may hold more than
    /// one reason, comma-separated — kept a plain string rather than a collection since this is
    /// display-only metadata, not something queried by individual reason.</summary>
    public bool RequiresApproval { get; private set; }

    public string? FlagReason { get; private set; }

    internal void ClearFlag()
    {
        RequiresApproval = false;
        FlagReason = null;
    }
}
