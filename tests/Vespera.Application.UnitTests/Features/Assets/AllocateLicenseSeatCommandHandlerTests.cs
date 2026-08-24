using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Assets;

public class AllocateLicenseSeatCommandHandlerTests
{
    private readonly IReadRepository<SoftwareLicense> _licenses = Substitute.For<IReadRepository<SoftwareLicense>>();
    private readonly IWriteRepository<SoftwareLicenseAllocation> _allocations = Substitute.For<IWriteRepository<SoftwareLicenseAllocation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public AllocateLicenseSeatCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private AllocateLicenseSeatCommandHandler CreateHandler() => new(_licenses, _allocations, _tenantContext, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Assign_Seat_And_Create_Allocation()
    {
        var license = SoftwareLicense.Create(_tenantId, "Figma", 1, null, DateTimeOffset.UtcNow, "system").Value;
        _licenses.FirstOrDefaultAsync(Arg.Any<SoftwareLicenseByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(license);

        var handler = CreateHandler();
        var result = await handler.Handle(new AllocateLicenseSeatCommand(license.Id.Value, Guid.NewGuid(), null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        license.SeatsUsed.Should().Be(1);
        await _allocations.Received(1).AddAsync(Arg.Any<SoftwareLicenseAllocation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_And_Not_Create_Allocation_When_No_Seats_Available()
    {
        var license = SoftwareLicense.Create(_tenantId, "Figma", 1, null, DateTimeOffset.UtcNow, "system").Value;
        license.AssignSeat();
        _licenses.FirstOrDefaultAsync(Arg.Any<SoftwareLicenseByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(license);

        var handler = CreateHandler();
        var result = await handler.Handle(new AllocateLicenseSeatCommand(license.Id.Value, Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _allocations.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
