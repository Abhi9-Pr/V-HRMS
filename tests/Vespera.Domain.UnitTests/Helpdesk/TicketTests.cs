using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;
using Vespera.Domain.Helpdesk.Events;

namespace Vespera.Domain.UnitTests.Helpdesk;

public class TicketTests
{
    private static readonly DateTimeOffset RaisedAt = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CheckSlaBreach_Should_Raise_TicketSlaBreached_Once_Past_Due()
    {
        var ticket = CreateTicket();

        var result = ticket.CheckSlaBreach(RaisedAt.AddHours(25));

        result.IsSuccess.Should().BeTrue();
        ticket.DomainEvents.Should().ContainSingle(e => e is TicketSlaBreached);
    }

    [Fact]
    public void CheckSlaBreach_Should_Not_Raise_Before_The_Due_Time()
    {
        var ticket = CreateTicket();

        ticket.CheckSlaBreach(RaisedAt.AddHours(1));

        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CheckSlaBreach_Should_Not_Raise_For_A_Resolved_Ticket()
    {
        var ticket = CreateTicket();
        ticket.Resolve(RaisedAt.AddHours(2));

        ticket.CheckSlaBreach(RaisedAt.AddHours(25));

        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Close_Should_Fail_Before_The_Ticket_Is_Resolved()
    {
        var ticket = CreateTicket();

        var result = ticket.Close();

        result.IsFailure.Should().BeTrue();
    }

    private static Ticket CreateTicket() =>
        Ticket.Raise(
            TenantId.New(), EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Laptop not booting",
            "Won't power on.", TicketPriority.High, RaisedAt, TimeSpan.FromHours(24)).Value;
}
