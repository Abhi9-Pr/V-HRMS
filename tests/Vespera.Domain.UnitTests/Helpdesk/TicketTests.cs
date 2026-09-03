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

    [Fact]
    public void Raise_With_An_Explicit_DueAt_Should_Use_It_Verbatim()
    {
        var dueAt = RaisedAt.AddHours(10);

        var ticket = Ticket.Raise(
            TenantId.New(), EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "VPN not connecting",
            "Can't reach the office VPN.", TicketPriority.Medium, RaisedAt, dueAt).Value;

        ticket.DueAt.Should().Be(dueAt);
    }

    [Fact]
    public void CheckSlaBreach_Called_Twice_Should_Only_Raise_The_Event_Once()
    {
        var ticket = CreateTicket();

        ticket.CheckSlaBreach(RaisedAt.AddHours(25));
        ticket.ClearDomainEvents();
        ticket.CheckSlaBreach(RaisedAt.AddHours(26));

        ticket.DomainEvents.Should().BeEmpty();
        ticket.SlaBreachNotified.Should().BeTrue();
    }

    [Fact]
    public void CheckSlaWarning_Should_Raise_Once_Past_The_Threshold_But_Before_Due()
    {
        var ticket = CreateTicket();

        // 80% of a 24h window is 19.2h in.
        var result = ticket.CheckSlaWarning(RaisedAt.AddHours(20));

        result.IsSuccess.Should().BeTrue();
        ticket.DomainEvents.Should().ContainSingle(e => e is TicketSlaWarningRaised);
        ticket.SlaWarningNotified.Should().BeTrue();
    }

    [Fact]
    public void CheckSlaWarning_Should_Not_Raise_Before_The_Threshold()
    {
        var ticket = CreateTicket();

        ticket.CheckSlaWarning(RaisedAt.AddHours(5));

        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CheckSlaWarning_Should_Not_Raise_Once_Already_Breached()
    {
        var ticket = CreateTicket();

        ticket.CheckSlaWarning(RaisedAt.AddHours(25));

        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RateSatisfaction_Should_Fail_Before_The_Ticket_Is_Closed()
    {
        var ticket = CreateTicket();

        var result = ticket.RateSatisfaction(5);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RateSatisfaction_Should_Succeed_Once_Closed()
    {
        var ticket = CreateTicket();
        ticket.Resolve(RaisedAt.AddHours(2));
        ticket.Close();

        var result = ticket.RateSatisfaction(4);

        result.IsSuccess.Should().BeTrue();
        ticket.SatisfactionRating.Should().Be(4);
    }

    [Fact]
    public void RateSatisfaction_Should_Reject_A_Rating_Outside_1_To_5()
    {
        var ticket = CreateTicket();
        ticket.Resolve(RaisedAt.AddHours(2));
        ticket.Close();

        var result = ticket.RateSatisfaction(6);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddComment_Should_Support_A_Threaded_Reply_With_Attachments()
    {
        var ticket = CreateTicket();
        var author = EmployeeId.New();
        ticket.AddComment(author, "Original note", isInternal: true, RaisedAt.AddMinutes(1));
        var parentId = ticket.Comments.Single().Id;

        var result = ticket.AddComment(
            author, "Following up", isInternal: false, RaisedAt.AddMinutes(2), parentId, ["storage/attachment-1.png"]);

        result.IsSuccess.Should().BeTrue();
        var reply = ticket.Comments.Single(c => c.ParentCommentId == parentId);
        reply.AttachmentReferences.Should().ContainSingle().Which.Should().Be("storage/attachment-1.png");
    }

    [Fact]
    public void AddComment_Should_Fail_When_The_Parent_Comment_Does_Not_Exist_On_This_Ticket()
    {
        var ticket = CreateTicket();

        var result = ticket.AddComment(
            EmployeeId.New(), "Reply to nothing", isInternal: false, RaisedAt.AddMinutes(1), new TicketCommentId(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Raise_Should_Populate_All_Fields_And_Default_To_Open()
    {
        var tenantId = TenantId.New();
        var raisedBy = EmployeeId.New();
        var categoryId = TicketCategoryId.New();
        var slaPolicyId = SlaPolicyId.New();

        var ticket = Ticket.Raise(
            tenantId, raisedBy, categoryId, slaPolicyId, "Laptop not booting", "Won't power on.",
            TicketPriority.High, RaisedAt, TimeSpan.FromHours(24)).Value;

        ticket.TenantId.Should().Be(tenantId);
        ticket.RaisedBy.Should().Be(raisedBy);
        ticket.CategoryId.Should().Be(categoryId);
        ticket.SlaPolicyId.Should().Be(slaPolicyId);
        ticket.Subject.Should().Be("Laptop not booting");
        ticket.Description.Should().Be("Won't power on.");
        ticket.Priority.Should().Be(TicketPriority.High);
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.AssignedTo.Should().BeNull();
        ticket.DueAt.Should().Be(RaisedAt.AddHours(24));
    }

    [Fact]
    public void Raise_With_A_ResolutionTime_Should_Fail_When_Subject_Is_Empty()
    {
        var result = Ticket.Raise(
            TenantId.New(), EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "  ",
            "Won't power on.", TicketPriority.High, RaisedAt, TimeSpan.FromHours(24));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.subject_required");
    }

    [Fact]
    public void Raise_With_An_Explicit_DueAt_Should_Fail_When_Subject_Is_Empty()
    {
        var result = Ticket.Raise(
            TenantId.New(), EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), string.Empty,
            "Won't power on.", TicketPriority.High, RaisedAt, RaisedAt.AddHours(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.subject_required");
    }

    [Fact]
    public void AddComment_Should_Fail_When_The_Body_Is_Empty()
    {
        var ticket = CreateTicket();

        var result = ticket.AddComment(EmployeeId.New(), "   ", isInternal: false, RaisedAt.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.comment_required");
    }

    [Fact]
    public void AssignTo_Should_Set_The_Assignee_And_Move_To_InProgress()
    {
        var ticket = CreateTicket();
        var assignee = EmployeeId.New();

        var result = ticket.AssignTo(assignee);

        result.IsSuccess.Should().BeTrue();
        ticket.AssignedTo.Should().Be(assignee);
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void AssignTo_Should_Fail_When_The_Ticket_Is_Already_Resolved_Or_Closed()
    {
        var ticket = CreateTicket();
        ticket.Resolve(RaisedAt.AddHours(2));

        var result = ticket.AssignTo(EmployeeId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.closed");
    }

    [Fact]
    public void Resolve_Should_Fail_When_The_Ticket_Is_Already_Resolved()
    {
        var ticket = CreateTicket();
        ticket.Resolve(RaisedAt.AddHours(2));

        var result = ticket.Resolve(RaisedAt.AddHours(3));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.already_resolved");
    }

    [Fact]
    public void CheckSlaWarning_Should_Not_Raise_For_A_Resolved_Ticket()
    {
        var ticket = CreateTicket();
        ticket.Resolve(RaisedAt.AddHours(2));

        var result = ticket.CheckSlaWarning(RaisedAt.AddHours(20));

        result.IsSuccess.Should().BeTrue();
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CheckSlaWarning_Called_Twice_Should_Only_Raise_The_Event_Once()
    {
        var ticket = CreateTicket();

        ticket.CheckSlaWarning(RaisedAt.AddHours(20));
        ticket.ClearDomainEvents();
        ticket.CheckSlaWarning(RaisedAt.AddHours(21));

        ticket.DomainEvents.Should().BeEmpty();
        ticket.SlaWarningNotified.Should().BeTrue();
    }

    [Fact]
    public void TicketId_New_Should_Generate_Distinct_Values()
    {
        TicketId.New().Should().NotBe(TicketId.New());
    }

    private static Ticket CreateTicket() =>
        Ticket.Raise(
            TenantId.New(), EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Laptop not booting",
            "Won't power on.", TicketPriority.High, RaisedAt, TimeSpan.FromHours(24)).Value;
}
