using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class CreateShiftCommandHandlerTests
{
    private readonly IWriteRepository<Shift> _shifts = Substitute.For<IWriteRepository<Shift>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateShiftCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateShiftCommandHandler CreateHandler() => new(_shifts, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_Shift_With_BreakMinutes_And_Return_Its_Id_On_Success()
    {
        var handler = CreateHandler();
        var command = new CreateShiftCommand("Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, 45, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _shifts.Received(1).AddAsync(
            Arg.Is<Shift>(s => s.Name == "Day Shift" && s.BreakMinutes == 45), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Name_Is_Blank()
    {
        var handler = CreateHandler();
        var command = new CreateShiftCommand(" ", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _shifts.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
