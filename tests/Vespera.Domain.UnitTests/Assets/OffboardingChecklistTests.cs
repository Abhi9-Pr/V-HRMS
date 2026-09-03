using FluentAssertions;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using OffboardingChecklist = Vespera.Domain.Assets.OffboardingChecklist;

namespace Vespera.Domain.UnitTests.Assets;

public class OffboardingChecklistTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();

    [Fact]
    public void Create_Should_Stage_One_Item_Per_Task_Description_All_Incomplete()
    {
        var checklist = OffboardingChecklist.Create(TenantId, EmployeeId, ["Return laptop", "Revoke badge"]);

        checklist.Items.Should().HaveCount(2);
        checklist.Items.Should().OnlyContain(item => !item.IsComplete);
        checklist.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void IsComplete_Should_Be_False_When_The_Checklist_Has_No_Items()
    {
        var checklist = OffboardingChecklist.Create(TenantId, EmployeeId, []);

        checklist.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void CompleteItem_Should_Mark_The_Item_Complete()
    {
        var checklist = OffboardingChecklist.Create(TenantId, EmployeeId, ["Return laptop"]);

        var result = checklist.CompleteItem(0);

        result.IsSuccess.Should().BeTrue();
        checklist.Items[0].IsComplete.Should().BeTrue();
    }

    [Fact]
    public void IsComplete_Should_Be_True_Once_Every_Item_Is_Complete()
    {
        var checklist = OffboardingChecklist.Create(TenantId, EmployeeId, ["Return laptop", "Revoke badge"]);

        checklist.CompleteItem(0);
        checklist.IsComplete.Should().BeFalse();

        checklist.CompleteItem(1);
        checklist.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void CompleteItem_Should_Fail_For_An_OutOfRange_Index()
    {
        var checklist = OffboardingChecklist.Create(TenantId, EmployeeId, ["Return laptop"]);

        var result = checklist.CompleteItem(5);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offboarding_checklist.item_not_found");
    }

    [Fact]
    public void CompleteItem_Should_Fail_When_Already_Complete()
    {
        var checklist = OffboardingChecklist.Create(TenantId, EmployeeId, ["Return laptop"]);
        checklist.CompleteItem(0);

        var result = checklist.CompleteItem(0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offboarding_checklist.already_complete");
    }

    [Fact]
    public void Items_With_The_Same_Description_And_Completion_State_Should_Be_Equal()
    {
        var checklistA = OffboardingChecklist.Create(TenantId, EmployeeId, ["Return laptop"]);
        var checklistB = OffboardingChecklist.Create(TenantId, EmployeeId, ["Return laptop"]);

        checklistA.Items[0].Should().Be(checklistB.Items[0]);

        checklistA.CompleteItem(0);
        checklistA.Items[0].Should().NotBe(checklistB.Items[0]);
    }
}
