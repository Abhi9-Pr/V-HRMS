using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Rosters;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Rosters;

public class OverrideRosterAssignmentCommandHandlerTests
{
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly IWriteRepository<ShiftRoster> _rosters = Substitute.For<IWriteRepository<ShiftRoster>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public OverrideRosterAssignmentCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(Domain.Common.TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private OverrideRosterAssignmentCommandHandler CreateHandler() =>
        new(_shifts, _rosters, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Create_A_Published_Override_Row_When_Shift_Exists()
    {
        _shifts.AnyAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(true);

        var command = new OverrideRosterAssignmentCommand(Guid.NewGuid(), new DateOnly(2026, 3, 10), Guid.NewGuid());
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _rosters.Received(1).AddAsync(
            Arg.Is<ShiftRoster>(r => r.IsOverride && r.Status == ShiftRosterStatus.Published),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Shift_Does_Not_Exist()
    {
        _shifts.AnyAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new OverrideRosterAssignmentCommand(Guid.NewGuid(), new DateOnly(2026, 3, 10), Guid.NewGuid());
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("shift.not_found");
        await _rosters.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
