using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Workspace;
using Vespera.Application.UnitTests.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class RespondToEventRsvpCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<EventRsvp> _rsvps = Substitute.For<IReadRepository<EventRsvp>>();
    private readonly IWriteRepository<EventRsvp> _rsvpWriter = Substitute.For<IWriteRepository<EventRsvp>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public RespondToEventRsvpCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private RespondToEventRsvpCommandHandler CreateHandler() => new(
        _rsvps, _rsvpWriter, _tenantContext, _dateTimeProvider, CurrentEmployeeTestSupport.CreateResolver(TenantId, EmployeeId));

    [Fact]
    public async Task Handle_Should_Fail_When_The_Signed_In_User_Has_No_Linked_Employee()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var resolver = new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), currentUser);
        var handler = new RespondToEventRsvpCommandHandler(_rsvps, _rsvpWriter, _tenantContext, _dateTimeProvider, resolver);

        var result = await handler.Handle(new RespondToEventRsvpCommand(Guid.NewGuid(), RsvpResponse.Yes), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("event_rsvp.no_employee");
    }

    [Fact]
    public async Task Handle_Should_Create_A_New_Rsvp_When_None_Exists()
    {
        _rsvps.FirstOrDefaultAsync(Arg.Any<EventRsvpByEventAndEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((EventRsvp?)null);

        var result = await CreateHandler().Handle(new RespondToEventRsvpCommand(Guid.NewGuid(), RsvpResponse.Yes), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _rsvpWriter.Received(1).AddAsync(
            Arg.Is<EventRsvp>(r => r.EmployeeId == EmployeeId && r.Response == RsvpResponse.Yes), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Update_An_Existing_Rsvp()
    {
        var corporateEventId = new CorporateEventId(Guid.NewGuid());
        var rsvp = EventRsvp.Create(TenantId, corporateEventId, EmployeeId);
        rsvp.Respond(RsvpResponse.Maybe, Now.AddDays(-1));
        _rsvps.FirstOrDefaultAsync(Arg.Any<EventRsvpByEventAndEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(rsvp);

        var result = await CreateHandler().Handle(new RespondToEventRsvpCommand(corporateEventId.Value, RsvpResponse.No), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        rsvp.Response.Should().Be(RsvpResponse.No);
        _rsvpWriter.Received(1).Update(rsvp);
    }
}
