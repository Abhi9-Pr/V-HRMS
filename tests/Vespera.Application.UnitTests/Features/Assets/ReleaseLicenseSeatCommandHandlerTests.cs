using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Assets;

public class ReleaseLicenseSeatCommandHandlerTests
{
    private readonly IReadRepository<SoftwareLicenseAllocation> _allocations = Substitute.For<IReadRepository<SoftwareLicenseAllocation>>();
    private readonly IReadRepository<SoftwareLicense> _licenses = Substitute.For<IReadRepository<SoftwareLicense>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public ReleaseLicenseSeatCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private ReleaseLicenseSeatCommandHandler CreateHandler() => new(_allocations, _licenses, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Release_Allocation_And_Seat()
    {
        var license = SoftwareLicense.Create(_tenantId, "Figma", 1, null, DateTimeOffset.UtcNow, "system").Value;
        license.AssignSeat();
        var allocation = SoftwareLicenseAllocation.Allocate(_tenantId, license.Id, EmployeeId.New(), DateTimeOffset.UtcNow);

        _allocations.FirstOrDefaultAsync(Arg.Any<SoftwareLicenseAllocationByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(allocation);
        _licenses.FirstOrDefaultAsync(Arg.Any<SoftwareLicenseByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(license);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReleaseLicenseSeatCommand(allocation.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        allocation.IsActive.Should().BeFalse();
        license.SeatsUsed.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Allocation_Not_Found()
    {
        _allocations.FirstOrDefaultAsync(Arg.Any<SoftwareLicenseAllocationByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((SoftwareLicenseAllocation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReleaseLicenseSeatCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
