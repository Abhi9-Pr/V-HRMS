using FluentAssertions;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Assets;

public class SoftwareLicenseAllocationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Allocate_Should_Be_Active()
    {
        var allocation = SoftwareLicenseAllocation.Allocate(TenantId.New(), SoftwareLicenseId.New(), EmployeeId.New(), Now);

        allocation.IsActive.Should().BeTrue();
        allocation.ReleasedAt.Should().BeNull();
    }

    [Fact]
    public void Release_Should_Set_ReleasedAt_And_Become_Inactive()
    {
        var allocation = SoftwareLicenseAllocation.Allocate(TenantId.New(), SoftwareLicenseId.New(), EmployeeId.New(), Now);

        var result = allocation.Release(Now.AddDays(30));

        result.IsSuccess.Should().BeTrue();
        allocation.IsActive.Should().BeFalse();
        allocation.ReleasedAt.Should().Be(Now.AddDays(30));
    }

    [Fact]
    public void Release_Should_Fail_When_Already_Released()
    {
        var allocation = SoftwareLicenseAllocation.Allocate(TenantId.New(), SoftwareLicenseId.New(), EmployeeId.New(), Now);
        allocation.Release(Now.AddDays(30));

        var result = allocation.Release(Now.AddDays(31));

        result.IsFailure.Should().BeTrue();
    }
}
