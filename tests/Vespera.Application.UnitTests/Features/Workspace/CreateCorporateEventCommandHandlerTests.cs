using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class CreateCorporateEventCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IWriteRepository<CorporateEvent> _events = Substitute.For<IWriteRepository<CorporateEvent>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateCorporateEventCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private CreateCorporateEventCommandHandler CreateHandler() => new(_events, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Create_And_Add_The_Event()
    {
        var command = new CreateCorporateEventCommand("All-hands", "Quarterly all-hands", Now.AddDays(1), Now.AddDays(1).AddHours(2), "HQ", null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _events.Received(1).AddAsync(Arg.Is<CorporateEvent>(e => e.Title == "All-hands"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Event_Window_Is_Invalid()
    {
        var command = new CreateCorporateEventCommand("All-hands", "Quarterly all-hands", Now.AddDays(1), Now.AddDays(1), "HQ", null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _events.DidNotReceive().AddAsync(Arg.Any<CorporateEvent>(), Arg.Any<CancellationToken>());
    }
}
