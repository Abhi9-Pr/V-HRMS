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

public class GetUpcomingCorporateEventsQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<CorporateEvent> _events = Substitute.For<IReadRepository<CorporateEvent>>();
    private readonly IReadRepository<EventRsvp> _rsvps = Substitute.For<IReadRepository<EventRsvp>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public GetUpcomingCorporateEventsQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private GetUpcomingCorporateEventsQueryHandler CreateHandler() => new(
        _events, _rsvps, _tenantContext, _dateTimeProvider, CurrentEmployeeTestSupport.CreateResolver(TenantId, EmployeeId));

    private static CorporateEvent CreateEvent() => CorporateEvent.Create(
        TenantId, "All-hands", "Quarterly all-hands", Now.AddDays(1), Now.AddDays(1).AddHours(2), "HQ", Now, "hr@vespera.test").Value;

    [Fact]
    public async Task Handle_Should_Leave_MyResponse_Null_When_The_Signed_In_User_Has_No_Linked_Employee()
    {
        var corporateEvent = CreateEvent();
        _events.ListAsync(Arg.Any<UpcomingCorporateEventsSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CorporateEvent>)[corporateEvent]);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var resolver = new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), currentUser);
        var handler = new GetUpcomingCorporateEventsQueryHandler(_events, _rsvps, _tenantContext, _dateTimeProvider, resolver);

        var result = await handler.Handle(new GetUpcomingCorporateEventsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == corporateEvent.Id.Value && dto.MyResponse == null);
    }

    [Fact]
    public async Task Handle_Should_Populate_MyResponse_From_The_Employees_Rsvp()
    {
        var corporateEvent = CreateEvent();
        _events.ListAsync(Arg.Any<UpcomingCorporateEventsSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CorporateEvent>)[corporateEvent]);

        var rsvp = EventRsvp.Create(TenantId, corporateEvent.Id, EmployeeId);
        rsvp.Respond(RsvpResponse.Yes, Now);
        _rsvps.ListAsync(Arg.Any<EventRsvpsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventRsvp>)[rsvp]);

        var result = await CreateHandler().Handle(new GetUpcomingCorporateEventsQuery(), CancellationToken.None);

        result.Value.Should().ContainSingle(dto => dto.Id == corporateEvent.Id.Value && dto.MyResponse == "Yes");
    }

    [Fact]
    public async Task Handle_Should_Leave_MyResponse_Null_When_The_Employee_Has_Not_Responded_To_This_Event()
    {
        var corporateEvent = CreateEvent();
        _events.ListAsync(Arg.Any<UpcomingCorporateEventsSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<CorporateEvent>)[corporateEvent]);
        _rsvps.ListAsync(Arg.Any<EventRsvpsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventRsvp>)[]);

        var result = await CreateHandler().Handle(new GetUpcomingCorporateEventsQuery(), CancellationToken.None);

        result.Value.Should().ContainSingle(dto => dto.Id == corporateEvent.Id.Value && dto.MyResponse == null);
    }
}
