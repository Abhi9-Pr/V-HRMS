using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Holidays;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Holidays;

public class GetHolidaysQueryHandlerTests
{
    private readonly IReadRepository<Holiday> _holidays = Substitute.For<IReadRepository<Holiday>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var holiday = Holiday.Create(
            tenantId, LocationId.New(), new DateOnly(2026, 1, 26), "Republic Day", DateTimeOffset.UtcNow, "system").Value;

        _holidays.ListAsync(Arg.Any<ISpecification<Holiday>>(), Arg.Any<CancellationToken>()).Returns(new List<Holiday> { holiday });
        _holidays.CountAsync(Arg.Any<ISpecification<Holiday>>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GetHolidaysQueryHandler(_holidays, _tenantContext);
        var result = await handler.Handle(new GetHolidaysQuery(new PagedRequest(), null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Name == "Republic Day" && dto.Id == holiday.Id.Value);
    }
}
