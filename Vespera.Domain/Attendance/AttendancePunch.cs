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
    internal AttendancePunch(AttendancePunchId id, PunchType punchType, DateTimeOffset punchedAtUtc, GeoCoordinate? location, PunchSource source)
        : base(id)
    {
        PunchType = punchType;
        PunchedAtUtc = punchedAtUtc;
        Location = location;
        Source = source;
    }

    public PunchType PunchType { get; }

    public DateTimeOffset PunchedAtUtc { get; }

    public GeoCoordinate? Location { get; }

    public PunchSource Source { get; }
}
