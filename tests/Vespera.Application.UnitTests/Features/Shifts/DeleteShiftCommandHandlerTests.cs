using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class DeleteShiftCommandHandlerTests
{
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public DeleteShiftCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private DeleteShiftCommandHandler CreateHandler() => new(_shifts, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Soft_Delete_When_Shift_Exists()
    {
        var shift = Shift.Create(_tenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, DateTimeOffset.UtcNow, "seed").Value;
        _shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteShiftCommand(shift.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        shift.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Shift_Missing()
    {
        _shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns((Shift?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteShiftCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("shift.not_found");
    }
}
