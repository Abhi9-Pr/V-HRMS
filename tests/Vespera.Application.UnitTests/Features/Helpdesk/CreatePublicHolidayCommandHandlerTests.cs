using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class CreatePublicHolidayCommandHandlerTests
{
    private readonly IWriteRepository<PublicHoliday> _holidays = Substitute.For<IWriteRepository<PublicHoliday>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private CreatePublicHolidayCommandHandler CreateHandler() => new(_holidays, _tenantContext);

    [Fact]
    public async Task Handle_Should_Stage_A_New_Public_Holiday_And_Return_Its_Id()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var result = await CreateHandler().Handle(new CreatePublicHolidayCommand(new DateOnly(2026, 1, 26), "Republic Day", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _holidays.Received(1).AddAsync(Arg.Is<PublicHoliday>(h => h.Name == "Republic Day"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Name_Is_Blank()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var result = await CreateHandler().Handle(new CreatePublicHolidayCommand(new DateOnly(2026, 1, 26), "   ", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("public_holiday.name_required");
        await _holidays.DidNotReceive().AddAsync(Arg.Any<PublicHoliday>(), Arg.Any<CancellationToken>());
    }
}
