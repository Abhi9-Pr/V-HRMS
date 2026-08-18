using FluentAssertions;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Assets;

public class SoftwareLicenseTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AssignSeat_Should_Fail_When_All_Seats_Are_Used()
    {
        var license = SoftwareLicense.Create(TenantId.New(), "Figma", seatCount: 1, expiresAt: null, Now, "admin@vespera.test").Value;
        license.AssignSeat();

        var result = license.AssignSeat();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReleaseSeat_Then_AssignSeat_Should_Allow_Reuse()
    {
        var license = SoftwareLicense.Create(TenantId.New(), "Figma", seatCount: 1, expiresAt: null, Now, "admin@vespera.test").Value;
        license.AssignSeat();
        license.ReleaseSeat();

        var result = license.AssignSeat();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ReleaseSeat_Should_Fail_When_No_Seats_Are_In_Use()
    {
        var license = SoftwareLicense.Create(TenantId.New(), "Figma", seatCount: 1, expiresAt: null, Now, "admin@vespera.test").Value;

        var result = license.ReleaseSeat();

        result.IsFailure.Should().BeTrue();
    }
}
