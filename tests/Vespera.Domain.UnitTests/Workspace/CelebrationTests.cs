using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Domain.UnitTests.Workspace;

public class CelebrationTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateOnly BirthDate = new(1990, 6, 15);

    [Fact]
    public void Create_Should_Set_Properties()
    {
        var celebration = Celebration.Create(TenantId, EmployeeId, CelebrationType.Birthday, BirthDate);

        celebration.TenantId.Should().Be(TenantId);
        celebration.EmployeeId.Should().Be(EmployeeId);
        celebration.CelebrationType.Should().Be(CelebrationType.Birthday);
        celebration.Date.Should().Be(BirthDate);
    }

    [Fact]
    public void Reschedule_Should_Update_Date()
    {
        var celebration = Celebration.Create(TenantId, EmployeeId, CelebrationType.WorkAnniversary, BirthDate);
        var newDate = new DateOnly(1990, 7, 1);

        var result = celebration.Reschedule(newDate);

        result.IsSuccess.Should().BeTrue();
        celebration.Date.Should().Be(newDate);
    }
}
