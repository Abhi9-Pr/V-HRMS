using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class GetShiftsQueryHandlerTests
{
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var shift = Shift.Create(tenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, DateTimeOffset.UtcNow, "system").Value;

        _shifts.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(new List<Shift> { shift });
        _shifts.CountAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GetShiftsQueryHandler(_shifts, _tenantContext);
        var result = await handler.Handle(new GetShiftsQuery(new PagedRequest()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Name == "Day Shift" && dto.Id == shift.Id.Value);
    }
}
