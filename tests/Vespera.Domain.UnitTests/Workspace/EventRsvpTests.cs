using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Domain.UnitTests.Workspace;

public class EventRsvpTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly CorporateEventId CorporateEventId = CorporateEventId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Leave_Response_Unset()
    {
        var rsvp = CreateRsvp();

        rsvp.TenantId.Should().Be(TenantId);
        rsvp.CorporateEventId.Should().Be(CorporateEventId);
        rsvp.EmployeeId.Should().Be(EmployeeId);
        rsvp.Response.Should().BeNull();
        rsvp.RespondedAt.Should().BeNull();
    }

    [Fact]
    public void Respond_Should_Set_Response_And_RespondedAt()
    {
        var rsvp = CreateRsvp();

        var result = rsvp.Respond(RsvpResponse.Yes, Now);

        result.IsSuccess.Should().BeTrue();
        rsvp.Response.Should().Be(RsvpResponse.Yes);
        rsvp.RespondedAt.Should().Be(Now);
    }

    [Fact]
    public void Respond_Should_Overwrite_A_Prior_Response()
    {
        var rsvp = CreateRsvp();
        rsvp.Respond(RsvpResponse.Maybe, Now);

        var result = rsvp.Respond(RsvpResponse.No, Now.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        rsvp.Response.Should().Be(RsvpResponse.No);
        rsvp.RespondedAt.Should().Be(Now.AddHours(1));
    }

    private static EventRsvp CreateRsvp() => EventRsvp.Create(TenantId, CorporateEventId, EmployeeId);
}
