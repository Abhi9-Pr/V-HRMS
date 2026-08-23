using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class GetShiftByIdQueryHandlerTests
{
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetShiftByIdQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Fact]
    public async Task Handle_Should_Return_Mapped_Dto_When_Shift_Exists()
    {
        var shift = Shift.Create(_tenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, DateTimeOffset.UtcNow, "seed").Value;
        shift.ConfigureBreak(30, DateTimeOffset.UtcNow, "seed");
        _shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(shift);

        var handler = new GetShiftByIdQueryHandler(_shifts, _tenantContext);
        var result = await handler.Handle(new GetShiftByIdQuery(shift.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Day Shift");
        result.Value.BreakMinutes.Should().Be(30);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Shift_Missing()
    {
        _shifts.FirstOrDefaultAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns((Shift?)null);

        var handler = new GetShiftByIdQueryHandler(_shifts, _tenantContext);
        var result = await handler.Handle(new GetShiftByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("shift.not_found");
    }
}
