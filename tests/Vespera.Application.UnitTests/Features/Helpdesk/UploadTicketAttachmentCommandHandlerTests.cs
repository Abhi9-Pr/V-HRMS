using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class UploadTicketAttachmentCommandHandlerTests
{
    private readonly IReadRepository<Ticket> _tickets = Substitute.For<IReadRepository<Ticket>>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

    private UploadTicketAttachmentCommandHandler CreateHandler() => new(_tickets, _fileStorage);

    private Ticket CreateTicket()
    {
        var ticket = Ticket.Raise(
            _tenantId, EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, Now, TimeSpan.FromHours(24)).Value;
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(ticket);
        return ticket;
    }

    [Fact]
    public async Task Handle_Should_Upload_The_Attachment_And_Return_Its_Storage_Reference()
    {
        var ticket = CreateTicket();
        _fileStorage.UploadAsync("photo.png", Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns("storage/photo.png");

        var result = await CreateHandler().Handle(
            new UploadTicketAttachmentCommand(ticket.Id.Value, [1, 2, 3], "photo.png", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("storage/photo.png");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Ticket_Does_Not_Exist()
    {
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        var result = await CreateHandler().Handle(
            new UploadTicketAttachmentCommand(Guid.NewGuid(), [1, 2, 3], "photo.png", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.not_found");
        await _fileStorage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default);
    }
}
