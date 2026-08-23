using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class UpdateShiftCommandHandlerTests
{
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateShiftCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UpdateShiftCommandHandler CreateHandler() => new(_shifts, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Rename_Reschedule_And_Configure_Break_When_Shift_Exists()
    {
        var shift = Shift.Create(_tenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, DateTimeOffset.UtcNow, "seed").Value;
        _shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);

        var handler = CreateHandler();
        var command = new UpdateShiftCommand(shift.Id.Value, "Evening Shift", new TimeOnly(14, 0), new TimeOnly(22, 0), 60);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        shift.Name.Should().Be("Evening Shift");
        shift.StartTime.Should().Be(new TimeOnly(14, 0));
        shift.BreakMinutes.Should().Be(60);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Shift_Missing()
    {
        _shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns((Shift?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateShiftCommand(Guid.NewGuid(), "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 0), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("shift.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_BreakMinutes_Is_Negative()
    {
        var shift = Shift.Create(_tenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, DateTimeOffset.UtcNow, "seed").Value;
        _shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);

        var handler = CreateHandler();
        var command = new UpdateShiftCommand(shift.Id.Value, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), -1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("shift.invalid_break");
    }
}
