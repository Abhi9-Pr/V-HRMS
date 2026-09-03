using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class CancelCorporateEventCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<CorporateEvent> _events = Substitute.For<IReadRepository<CorporateEvent>>();
    private readonly IWriteRepository<CorporateEvent> _eventWriter = Substitute.For<IWriteRepository<CorporateEvent>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CancelCorporateEventCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private CancelCorporateEventCommandHandler CreateHandler() => new(_events, _eventWriter, _tenantContext, _currentUser, _dateTimeProvider);

    private static CorporateEvent CreateEvent() => CorporateEvent.Create(
        TenantId, "All-hands", "Quarterly all-hands", Now.AddDays(1), Now.AddDays(1).AddHours(2), "HQ", Now, "hr@vespera.test").Value;

    [Fact]
    public async Task Handle_Should_Fail_When_The_Event_Does_Not_Exist()
    {
        _events.FirstOrDefaultAsync(Arg.Any<CorporateEventByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((CorporateEvent?)null);

        var result = await CreateHandler().Handle(new CancelCorporateEventCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("corporate_event.not_found");
    }

    [Fact]
    public async Task Handle_Should_Cancel_And_Save_The_Event()
    {
        var corporateEvent = CreateEvent();
        _events.FirstOrDefaultAsync(Arg.Any<CorporateEventByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(corporateEvent);

        var result = await CreateHandler().Handle(new CancelCorporateEventCommand(corporateEvent.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        corporateEvent.IsCancelled.Should().BeTrue();
        _eventWriter.Received(1).Update(corporateEvent);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Already_Cancelled()
    {
        var corporateEvent = CreateEvent();
        corporateEvent.Cancel(Now, "hr@vespera.test");
        _events.FirstOrDefaultAsync(Arg.Any<CorporateEventByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(corporateEvent);

        var result = await CreateHandler().Handle(new CancelCorporateEventCommand(corporateEvent.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _eventWriter.DidNotReceive().Update(Arg.Any<CorporateEvent>());
    }
}
