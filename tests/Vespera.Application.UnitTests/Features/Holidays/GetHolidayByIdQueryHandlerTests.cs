using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Holidays;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Holidays;

public class GetHolidayByIdQueryHandlerTests
{
    private readonly IReadRepository<Holiday> _holidays = Substitute.For<IReadRepository<Holiday>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetHolidayByIdQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Fact]
    public async Task Handle_Should_Return_Mapped_Dto_When_Holiday_Exists()
    {
        var locationId = LocationId.New();
        var holiday = Holiday.Create(
            _tenantId, locationId, new DateOnly(2026, 1, 26), "Republic Day", DateTimeOffset.UtcNow, "seed").Value;
        _holidays.FirstOrDefaultAsync(Arg.Any<ISpecification<Holiday>>(), Arg.Any<CancellationToken>()).Returns(holiday);

        var handler = new GetHolidayByIdQueryHandler(_holidays, _tenantContext);
        var result = await handler.Handle(new GetHolidayByIdQuery(holiday.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Republic Day");
        result.Value.LocationId.Should().Be(locationId.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Holiday_Missing()
    {
        _holidays.FirstOrDefaultAsync(Arg.Any<ISpecification<Holiday>>(), Arg.Any<CancellationToken>()).Returns((Holiday?)null);

        var handler = new GetHolidayByIdQueryHandler(_holidays, _tenantContext);
        var result = await handler.Handle(new GetHolidayByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("holiday.not_found");
    }
}
