using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class GetPublicHolidaysQueryHandlerTests
{
    private readonly IReadRepository<PublicHoliday> _holidays = Substitute.For<IReadRepository<PublicHoliday>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var holiday = PublicHoliday.Create(tenantId, new DateOnly(2026, 1, 26), "Republic Day").Value;

        _holidays.ListAsync(Arg.Any<ISpecification<PublicHoliday>>(), Arg.Any<CancellationToken>()).Returns([holiday]);
        _holidays.CountAsync(Arg.Any<ISpecification<PublicHoliday>>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GetPublicHolidaysQueryHandler(_holidays, _tenantContext);
        var result = await handler.Handle(new GetPublicHolidaysQuery(new PagedRequest()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Name == "Republic Day" && dto.Id == holiday.Id.Value);
    }
}
