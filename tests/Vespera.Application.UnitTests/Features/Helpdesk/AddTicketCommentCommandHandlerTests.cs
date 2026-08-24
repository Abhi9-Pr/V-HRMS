using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Helpdesk;
using Vespera.Application.UnitTests.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class AddTicketCommentCommandHandlerTests
{
    private readonly IWriteRepository<Ticket> _ticketWrites = Substitute.For<IWriteRepository<Ticket>>();
    private readonly IReadRepository<Ticket> _ticketReads = Substitute.For<IReadRepository<Ticket>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

    public AddTicketCommentCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private AddTicketCommentCommandHandler CreateHandler() =>
        new(_ticketWrites, _ticketReads, _dateTimeProvider, CurrentEmployeeTestSupport.CreateResolver(_tenantId, _employeeId));

    private Ticket CreateTicket()
    {
        var ticket = Ticket.Raise(
            _tenantId, EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, Now, TimeSpan.FromHours(24)).Value;
        _ticketReads.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(ticket);
        return ticket;
    }

    [Fact]
    public async Task Handle_Should_Add_A_Threaded_Comment_With_Attachments()
    {
        var ticket = CreateTicket();
        ticket.AddComment(EmployeeId.New(), "Original", isInternal: true, Now);
        var parentId = ticket.Comments.Single().Id;

        var handler = CreateHandler();
        var result = await handler.Handle(
            new AddTicketCommentCommand(ticket.Id.Value, "Reply", false, parentId.Value, ["storage/photo.png"], null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reply = ticket.Comments.Single(c => c.ParentCommentId == parentId);
        reply.AuthorId.Should().Be(_employeeId);
        reply.AttachmentReferences.Should().ContainSingle().Which.Should().Be("storage/photo.png");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Ticket_Does_Not_Exist()
    {
        _ticketReads.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new AddTicketCommentCommand(Guid.NewGuid(), "Body", false, null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
