using FluentAssertions;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeeDocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Start_Pending_Unverified_And_Unscanned()
    {
        var document = CreateDocument();

        document.VerificationStatus.Should().Be(DocumentVerificationStatus.Pending);
        document.ScanStatus.Should().Be(DocumentScanStatus.Pending);
        document.IsOcrConfirmed.Should().BeFalse();
    }

    [Fact]
    public void MarkScanned_Should_Update_ScanStatus()
    {
        var document = CreateDocument();

        var result = document.MarkScanned(DocumentScanStatus.Clean);

        result.IsSuccess.Should().BeTrue();
        document.ScanStatus.Should().Be(DocumentScanStatus.Clean);
    }

    [Fact]
    public void RecordOcrSuggestion_Should_Fail_When_FieldsJson_Is_Blank()
    {
        var document = CreateDocument();

        var result = document.RecordOcrSuggestion("  ", 0.5);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee_document.ocr_fields_required");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void RecordOcrSuggestion_Should_Fail_When_Confidence_Is_Out_Of_Range(double confidence)
    {
        var document = CreateDocument();

        var result = document.RecordOcrSuggestion("{\"firstName\":\"Ada\"}", confidence);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee_document.ocr_confidence_out_of_range");
    }

    [Fact]
    public void RecordOcrSuggestion_Should_Set_Fields_And_Confidence_When_Valid()
    {
        var document = CreateDocument();

        var result = document.RecordOcrSuggestion("{\"firstName\":\"Ada\"}", 0.9);

        result.IsSuccess.Should().BeTrue();
        document.OcrSuggestedFieldsJson.Should().Be("{\"firstName\":\"Ada\"}");
        document.OcrConfidence.Should().Be(0.9);
    }

    [Fact]
    public void ConfirmOcrSuggestion_Should_Fail_When_ConfirmedFieldsJson_Is_Blank()
    {
        var document = CreateDocument();

        var result = document.ConfirmOcrSuggestion("  ", "hr@vespera.test", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee_document.confirmed_fields_required");
    }

    [Fact]
    public void ConfirmOcrSuggestion_Should_Set_Confirmed_Fields_When_Valid()
    {
        var document = CreateDocument();

        var result = document.ConfirmOcrSuggestion("{\"firstName\":\"Ada\"}", "hr@vespera.test", Now);

        result.IsSuccess.Should().BeTrue();
        document.ConfirmedFieldsJson.Should().Be("{\"firstName\":\"Ada\"}");
        document.ConfirmedBy.Should().Be("hr@vespera.test");
        document.ConfirmedAt.Should().Be(Now);
        document.IsOcrConfirmed.Should().BeTrue();
    }

    [Fact]
    public void Verify_Should_Set_VerificationStatus_And_Clear_RejectionReason()
    {
        var document = CreateDocument();
        document.Reject("Blurry scan");

        var result = document.Verify();

        result.IsSuccess.Should().BeTrue();
        document.VerificationStatus.Should().Be(DocumentVerificationStatus.Verified);
        document.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Verify_Should_Fail_When_Already_Verified()
    {
        var document = CreateDocument();
        document.Verify();

        var result = document.Verify();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee_document.already_verified");
    }

    [Fact]
    public void Reject_Should_Fail_When_Reason_Is_Blank()
    {
        var document = CreateDocument();

        var result = document.Reject("  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee_document.rejection_reason_required");
    }

    [Fact]
    public void Reject_Should_Set_VerificationStatus_And_RejectionReason()
    {
        var document = CreateDocument();

        var result = document.Reject("  Blurry scan  ");

        result.IsSuccess.Should().BeTrue();
        document.VerificationStatus.Should().Be(DocumentVerificationStatus.Rejected);
        document.RejectionReason.Should().Be("Blurry scan");
    }

    [Fact]
    public void EmployeeDocumentId_Instances_With_The_Same_Value_Should_Be_Equal()
    {
        var value = Guid.NewGuid();

        new EmployeeDocumentId(value).Should().Be(new EmployeeDocumentId(value));
        EmployeeDocumentId.New().Should().NotBe(EmployeeDocumentId.New());
    }

    private static EmployeeDocument CreateDocument() =>
        new(EmployeeDocumentId.New(), EmployeeDocumentType.Id, "storage/id.pdf", Now);
}
