using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Eis.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeeTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Onboard_Should_Raise_EmployeeOnboarded_And_Seed_EmploymentHistory()
    {
        var employee = CreateEmployee();

        employee.DomainEvents.Should().ContainSingle(e => e is EmployeeOnboarded);
        employee.EmploymentHistory.Should().ContainSingle(h => h.ChangeReason == EmploymentChangeReason.Hire);
        employee.Status.Should().Be(EmploymentStatus.Active);
    }

    [Fact]
    public void Transfer_Should_Update_Assignment_And_Append_History()
    {
        var employee = CreateEmployee();
        var newDepartment = DepartmentId.New();
        var newDesignation = DesignationId.New();
        var newLocation = LocationId.New();

        var result = employee.Transfer(
            newDepartment, newDesignation, newLocation, new DateOnly(2026, 3, 1), EmploymentChangeReason.Transfer, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.DepartmentId.Should().Be(newDepartment);
        employee.EmploymentHistory.Should().HaveCount(2);
    }

    [Fact]
    public void Exit_Should_Raise_EmployeeExited_And_Set_Status()
    {
        var employee = CreateEmployee();

        var result = employee.Exit(new DateOnly(2026, 6, 30), EmployeeExitReason.Resignation, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.Status.Should().Be(EmploymentStatus.Exited);
        employee.DomainEvents.Should().ContainSingle(e => e is EmployeeExited);
    }

    [Fact]
    public void Exit_Should_Fail_When_Employee_Already_Exited()
    {
        var employee = CreateEmployee();
        employee.Exit(new DateOnly(2026, 6, 30), EmployeeExitReason.Resignation, Now, "hr@vespera.test");

        var result = employee.Exit(new DateOnly(2026, 7, 1), EmployeeExitReason.Resignation, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Transfer_Should_Fail_For_An_Exited_Employee()
    {
        var employee = CreateEmployee();
        employee.Exit(new DateOnly(2026, 6, 30), EmployeeExitReason.Resignation, Now, "hr@vespera.test");

        var result = employee.Transfer(
            DepartmentId.New(), DesignationId.New(), LocationId.New(), new DateOnly(2026, 7, 1), EmploymentChangeReason.Transfer, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Onboard_Should_Fail_When_FirstName_Is_Blank()
    {
        var code = EmployeeCode.Create("EMP-101").Value;
        var email = EmailAddress.Create("blank@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552671").Value;

        var result = Employee.Onboard(
            TenantId, code, "  ", "Lovelace", email, phone,
            new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
            DepartmentId.New(), DesignationId.New(), LocationId.New(), Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee.first_name_required");
    }

    [Fact]
    public void Onboard_Should_Fail_When_LastName_Is_Blank()
    {
        var code = EmployeeCode.Create("EMP-102").Value;
        var email = EmailAddress.Create("blank2@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552671").Value;

        var result = Employee.Onboard(
            TenantId, code, "Ada", "  ", email, phone,
            new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
            DepartmentId.New(), DesignationId.New(), LocationId.New(), Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee.last_name_required");
    }

    [Fact]
    public void UpdatePersonalDetails_Should_Fail_When_FirstName_Is_Blank()
    {
        var employee = CreateEmployee();
        var email = EmailAddress.Create("other@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552672").Value;

        var result = employee.UpdatePersonalDetails("  ", "Byron", email, phone, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee.first_name_required");
    }

    [Fact]
    public void UpdatePersonalDetails_Should_Fail_When_LastName_Is_Blank()
    {
        var employee = CreateEmployee();
        var email = EmailAddress.Create("other2@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552673").Value;

        var result = employee.UpdatePersonalDetails("Ada", "  ", email, phone, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee.last_name_required");
    }

    [Fact]
    public void UpdatePersonalDetails_Should_Update_Fields_When_Valid()
    {
        var employee = CreateEmployee();
        var email = EmailAddress.Create("updated@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552674").Value;

        var result = employee.UpdatePersonalDetails("Grace", "Hopper", email, phone, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.FirstName.Should().Be("Grace");
        employee.LastName.Should().Be("Hopper");
        employee.WorkEmail.Should().Be(email);
        employee.Phone.Should().Be(phone);
    }

    [Fact]
    public void SetGender_Should_Update_Gender()
    {
        var employee = CreateEmployee();

        var result = employee.SetGender(Gender.Female, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.Gender.Should().Be(Gender.Female);
    }

    [Fact]
    public void UpdateStatutoryDetails_Should_Set_Pan_And_BankAccount()
    {
        var employee = CreateEmployee();
        var pan = PanNumber.Create("ABCDE1234F").Value;
        var bankAccount = BankAccountNumber.Create("123456789012").Value;

        var result = employee.UpdateStatutoryDetails(pan, bankAccount, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.Pan.Should().Be(pan);
        employee.BankAccount.Should().Be(bankAccount);
    }

    [Fact]
    public void UpdateCompensation_Should_Set_CurrentAnnualCtc()
    {
        var employee = CreateEmployee();
        var ctc = Money.Of(1200000m, Currency.Inr);

        var result = employee.UpdateCompensation(ctc, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.CurrentAnnualCtc.Should().Be(ctc);
    }

    [Fact]
    public void AssignBiometricDeviceUserId_Should_Trim_And_Set_The_Value()
    {
        var employee = CreateEmployee();

        var result = employee.AssignBiometricDeviceUserId("  device-42  ", Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.BiometricDeviceUserId.Should().Be("device-42");
    }

    [Fact]
    public void AssignBiometricDeviceUserId_Should_Clear_The_Value_When_Blank()
    {
        var employee = CreateEmployee();
        employee.AssignBiometricDeviceUserId("device-42", Now, "hr@vespera.test");

        var result = employee.AssignBiometricDeviceUserId("   ", Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.BiometricDeviceUserId.Should().BeNull();
    }

    [Fact]
    public void SetCelebrationVisibility_Should_Update_The_Flag()
    {
        var employee = CreateEmployee();

        var result = employee.SetCelebrationVisibility(false, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.CelebrationsVisible.Should().BeFalse();
    }

    [Fact]
    public void AddDocument_Should_Append_To_Documents()
    {
        var employee = CreateEmployee();

        var document = employee.AddDocument(EmployeeDocumentType.Id, "storage/id.pdf", Now);

        employee.Documents.Should().ContainSingle(d => d.Id == document.Id);
    }

    [Fact]
    public void RemoveDocument_Should_Remove_An_Existing_Document()
    {
        var employee = CreateEmployee();
        var document = employee.AddDocument(EmployeeDocumentType.Id, "storage/id.pdf", Now);

        var result = employee.RemoveDocument(document.Id);

        result.IsSuccess.Should().BeTrue();
        employee.Documents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveDocument_Should_Fail_When_Document_Does_Not_Exist()
    {
        var employee = CreateEmployee();

        var result = employee.RemoveDocument(EmployeeDocumentId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee_document.not_found");
    }

    [Fact]
    public void RecordConsent_Should_Append_To_ConsentRecords()
    {
        var employee = CreateEmployee();

        var consent = employee.RecordConsent(ConsentType.DataProcessing, Now);

        employee.ConsentRecords.Should().ContainSingle(c => c.Id == consent.Id);
    }

    [Fact]
    public void Delete_Should_Set_IsDeleted_And_Audit_Fields()
    {
        var employee = CreateEmployee();

        var result = employee.Delete(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.IsDeleted.Should().BeTrue();
        employee.DeletedAt.Should().Be(Now);
        employee.DeletedBy.Should().Be("hr@vespera.test");
    }

    [Fact]
    public void Delete_Should_Fail_When_Already_Deleted()
    {
        var employee = CreateEmployee();
        employee.Delete(Now, "hr@vespera.test");

        var result = employee.Delete(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("entity.already_deleted");
    }

    [Fact]
    public void Restore_Should_Fail_When_Not_Deleted()
    {
        var employee = CreateEmployee();

        var result = employee.Restore();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("entity.not_deleted");
    }

    [Fact]
    public void Restore_Should_Clear_Deletion_Fields_When_Deleted()
    {
        var employee = CreateEmployee();
        employee.Delete(Now, "hr@vespera.test");

        var result = employee.Restore();

        result.IsSuccess.Should().BeTrue();
        employee.IsDeleted.Should().BeFalse();
        employee.DeletedAt.Should().BeNull();
        employee.DeletedBy.Should().BeNull();
    }

    private static Employee CreateEmployee()
    {
        var code = EmployeeCode.Create("EMP-100").Value;
        var email = EmailAddress.Create("employee@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552671").Value;

        return Employee.Onboard(
            TenantId, code, "Ada", "Lovelace", email, phone,
            new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
            DepartmentId.New(), DesignationId.New(), LocationId.New(),
            Now, "hr@vespera.test").Value;
    }
}
