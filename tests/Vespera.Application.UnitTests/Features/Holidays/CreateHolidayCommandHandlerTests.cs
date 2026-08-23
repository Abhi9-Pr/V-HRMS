using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Holidays;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Holidays;

public class CreateHolidayCommandHandlerTests
{
    private readonly IWriteRepository<Holiday> _holidays = Substitute.For<IWriteRepository<Holiday>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateHolidayCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateHolidayCommandHandler CreateHandler() => new(_holidays, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_Holiday_And_Return_Its_Id_On_Success()
    {
        var locationId = Guid.NewGuid();
        var handler = CreateHandler();
        var command = new CreateHolidayCommand(locationId, new DateOnly(2026, 1, 26), "Republic Day", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _holidays.Received(1).AddAsync(
            Arg.Is<Holiday>(h => h.Name == "Republic Day" && h.LocationId.Value == locationId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Name_Is_Blank()
    {
        var handler = CreateHandler();
        var command = new CreateHolidayCommand(Guid.NewGuid(), new DateOnly(2026, 1, 26), " ", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _holidays.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
