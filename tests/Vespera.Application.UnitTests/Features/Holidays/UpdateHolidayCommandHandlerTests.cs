using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Holidays;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Holidays;

public class UpdateHolidayCommandHandlerTests
{
    private readonly IReadRepository<Holiday> _holidays = Substitute.For<IReadRepository<Holiday>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateHolidayCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UpdateHolidayCommandHandler CreateHandler() => new(_holidays, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Rename_And_Reschedule_When_Holiday_Exists()
    {
        var holiday = Holiday.Create(
            _tenantId, LocationId.New(), new DateOnly(2026, 1, 26), "Republic Day", DateTimeOffset.UtcNow, "seed").Value;
        _holidays.FirstOrDefaultAsync(Arg.Any<ISpecification<Holiday>>(), Arg.Any<CancellationToken>()).Returns(holiday);

        var handler = CreateHandler();
        var command = new UpdateHolidayCommand(holiday.Id.Value, "Republic Day (Observed)", new DateOnly(2026, 1, 27));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        holiday.Name.Should().Be("Republic Day (Observed)");
        holiday.Date.Should().Be(new DateOnly(2026, 1, 27));
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Holiday_Missing()
    {
        _holidays.FirstOrDefaultAsync(Arg.Any<ISpecification<Holiday>>(), Arg.Any<CancellationToken>()).Returns((Holiday?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateHolidayCommand(Guid.NewGuid(), "Republic Day", new DateOnly(2026, 1, 26)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("holiday.not_found");
    }
}
