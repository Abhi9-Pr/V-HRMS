using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeeDocumentScanAndOcrTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_Document_Should_Start_With_Pending_ScanStatus()
    {
        var document = AddDocument();

        document.ScanStatus.Should().Be(DocumentScanStatus.Pending);
    }

    [Fact]
    public void MarkScanned_Should_Update_ScanStatus()
    {
        var document = AddDocument();

        var result = document.MarkScanned(DocumentScanStatus.Clean);

        result.IsSuccess.Should().BeTrue();
        document.ScanStatus.Should().Be(DocumentScanStatus.Clean);
    }

    [Fact]
    public void RecordOcrSuggestion_Should_Fail_When_Fields_Are_Blank()
    {
        var document = AddDocument();

        var result = document.RecordOcrSuggestion("   ", 0.9);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.ocr_fields_required");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void RecordOcrSuggestion_Should_Fail_When_Confidence_Out_Of_Range(double confidence)
    {
        var document = AddDocument();

        var result = document.RecordOcrSuggestion("{\"firstName\":\"Ada\"}", confidence);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.ocr_confidence_out_of_range");
    }

    [Fact]
    public void RecordOcrSuggestion_Should_Store_The_Suggestion_Without_Confirming_It()
    {
        var document = AddDocument();

        var result = document.RecordOcrSuggestion("{\"firstName\":\"Ada\"}", 0.87);

        result.IsSuccess.Should().BeTrue();
        document.OcrSuggestedFieldsJson.Should().Be("{\"firstName\":\"Ada\"}");
        document.OcrConfidence.Should().Be(0.87);
        document.IsOcrConfirmed.Should().BeFalse();
    }

    [Fact]
    public void ConfirmOcrSuggestion_Should_Store_The_Human_Confirmed_Values_Even_When_They_Differ_From_The_Suggestion()
    {
        var document = AddDocument();
        document.RecordOcrSuggestion("{\"firstName\":\"Ada\"}", 0.55);

        var result = document.ConfirmOcrSuggestion("{\"firstName\":\"Adaeze\"}", "hr@vespera.test", Now);

        result.IsSuccess.Should().BeTrue();
        document.IsOcrConfirmed.Should().BeTrue();
        document.ConfirmedFieldsJson.Should().Be("{\"firstName\":\"Adaeze\"}");
        document.OcrSuggestedFieldsJson.Should().Be("{\"firstName\":\"Ada\"}");
        document.ConfirmedBy.Should().Be("hr@vespera.test");
        document.ConfirmedAt.Should().Be(Now);
    }

    [Fact]
    public void ConfirmOcrSuggestion_Should_Fail_When_Confirmed_Fields_Are_Blank()
    {
        var document = AddDocument();

        var result = document.ConfirmOcrSuggestion(string.Empty, "hr@vespera.test", Now);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.confirmed_fields_required");
    }

    private static EmployeeDocument AddDocument()
    {
        var employee = CreateEmployee();
        return employee.AddDocument(EmployeeDocumentType.Id, "storage-key-1", Now);
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
