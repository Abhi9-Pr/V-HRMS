using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Eis;

public class OffboardingChecklistTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly ExitDate = new(2026, 1, 31);

    [Fact]
    public void Initiate_Should_Start_All_Items_Pending()
    {
        var result = OffboardingChecklist.Initiate(TenantId, EmployeeId, ExitDate, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        var checklist = result.Value;
        checklist.EmployeeId.Should().Be(EmployeeId);
        checklist.ExitDate.Should().Be(ExitDate);
        checklist.AccessRevokedStatus.Should().Be(ChecklistItemStatus.Pending);
        checklist.AssetsRecoveredStatus.Should().Be(ChecklistItemStatus.Pending);
        checklist.FinalSettlementStatus.Should().Be(ChecklistItemStatus.Pending);
        checklist.AccessRevokedAt.Should().BeNull();
        checklist.AssetsRecoveredAt.Should().BeNull();
        checklist.FinalSettlementAt.Should().BeNull();
    }

    [Fact]
    public void MarkAccessRevoked_Should_Set_Status_Done_And_Timestamp()
    {
        var checklist = CreateChecklist();

        var result = checklist.MarkAccessRevoked(Now, "system");

        result.IsSuccess.Should().BeTrue();
        checklist.AccessRevokedStatus.Should().Be(ChecklistItemStatus.Done);
        checklist.AccessRevokedAt.Should().Be(Now);
    }

    [Fact]
    public void MarkAccessRevoked_Should_Fail_When_Already_Done()
    {
        var checklist = CreateChecklist();
        checklist.MarkAccessRevoked(Now, "system");

        var result = checklist.MarkAccessRevoked(Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offboarding_checklist.access_already_revoked");
    }

    [Fact]
    public void MarkAssetsRecovered_Should_Set_Status_Done_And_Timestamp()
    {
        var checklist = CreateChecklist();

        var result = checklist.MarkAssetsRecovered(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        checklist.AssetsRecoveredStatus.Should().Be(ChecklistItemStatus.Done);
        checklist.AssetsRecoveredAt.Should().Be(Now);
    }

    [Fact]
    public void MarkAssetsRecovered_Should_Fail_When_Already_Done()
    {
        var checklist = CreateChecklist();
        checklist.MarkAssetsRecovered(Now, "hr@vespera.test");

        var result = checklist.MarkAssetsRecovered(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offboarding_checklist.assets_already_recovered");
    }

    [Fact]
    public void MarkFinalSettlementProcessed_Should_Set_Status_Done_And_Timestamp()
    {
        var checklist = CreateChecklist();

        var result = checklist.MarkFinalSettlementProcessed(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        checklist.FinalSettlementStatus.Should().Be(ChecklistItemStatus.Done);
        checklist.FinalSettlementAt.Should().Be(Now);
    }

    [Fact]
    public void MarkFinalSettlementProcessed_Should_Fail_When_Already_Done()
    {
        var checklist = CreateChecklist();
        checklist.MarkFinalSettlementProcessed(Now, "hr@vespera.test");

        var result = checklist.MarkFinalSettlementProcessed(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offboarding_checklist.final_settlement_already_processed");
    }

    [Fact]
    public void Items_Should_Be_Independent_Of_Each_Other()
    {
        var checklist = CreateChecklist();

        checklist.MarkAccessRevoked(Now, "system");

        checklist.AssetsRecoveredStatus.Should().Be(ChecklistItemStatus.Pending);
        checklist.FinalSettlementStatus.Should().Be(ChecklistItemStatus.Pending);
    }

    private static OffboardingChecklist CreateChecklist() =>
        OffboardingChecklist.Initiate(TenantId, EmployeeId, ExitDate, Now, "hr@vespera.test").Value;
}
