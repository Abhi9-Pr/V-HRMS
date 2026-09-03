using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class OnboardingDraftTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);
    private static readonly EmailAddress Email = EmailAddress.Create("newhire@vespera.test").Value;
    private static readonly PhoneNumber Phone = PhoneNumber.Create("+14155552671").Value;

    [Fact]
    public void StartDraft_Should_Start_In_Draft_Status_At_PersonalDetails_Step()
    {
        var draft = CreateDraft();

        draft.Status.Should().Be(OnboardingStatus.Draft);
        draft.CurrentStep.Should().Be(OnboardingStep.PersonalDetails);
    }

    [Fact]
    public void UpdatePersonalDetails_Should_Advance_Step_But_Never_Regress_It()
    {
        var draft = CreateDraft();

        draft.UpdatePersonalDetails("Ada", "Lovelace", Email, Phone, new DateOnly(1990, 1, 1), Now, "hr@vespera.test");
        draft.UpdateEmploymentDetails(DepartmentId.New(), DesignationId.New(), LocationId.New(), new DateOnly(2026, 2, 1), Now, "hr@vespera.test");
        draft.CurrentStep.Should().Be(OnboardingStep.Documents);

        // Revisiting the personal-details step must not walk CurrentStep back to EmploymentDetails.
        draft.UpdatePersonalDetails("Ada", "Byron", Email, Phone, new DateOnly(1990, 1, 1), Now, "hr@vespera.test");

        draft.CurrentStep.Should().Be(OnboardingStep.Documents);
        draft.LastName.Should().Be("Byron");
    }

    [Fact]
    public void UpdatePersonalDetails_Should_Fail_When_First_Name_Is_Blank()
    {
        var draft = CreateDraft();

        var result = draft.UpdatePersonalDetails("  ", "Lovelace", Email, Phone, new DateOnly(1990, 1, 1), Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Submit_Should_Fail_When_Personal_Details_Are_Incomplete()
    {
        var draft = CreateDraft();

        var result = draft.Submit(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("onboarding_draft.personal_details_incomplete");
    }

    [Fact]
    public void Submit_Should_Fail_When_Employment_Details_Are_Missing()
    {
        var draft = CreateDraft();
        draft.UpdatePersonalDetails("Ada", "Lovelace", Email, Phone, new DateOnly(1990, 1, 1), Now, "hr@vespera.test");

        var result = draft.Submit(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("onboarding_draft.employment_details_incomplete");
    }

    [Fact]
    public void Submit_Should_Fail_When_No_DataProcessing_Consent_Recorded()
    {
        var draft = CreateDraftThroughEmploymentDetails();
        draft.AddDocument(EmployeeDocumentType.Id, "storage/id.pdf", Now);

        var result = draft.Submit(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("onboarding_draft.consent_required");
    }

    [Fact]
    public void Submit_Should_Fail_When_No_Id_Document_Uploaded()
    {
        var draft = CreateDraftThroughEmploymentDetails();
        draft.RecordConsent(ConsentType.DataProcessing, Now);

        var result = draft.Submit(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("onboarding_draft.id_document_required");
    }

    [Fact]
    public void Submit_Should_Succeed_Once_Every_Precondition_Is_Met()
    {
        var draft = CreateCompleteDraft();

        var result = draft.Submit(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(OnboardingStatus.Submitted);
    }

    [Fact]
    public void ConvertToEmployee_Should_Fail_When_Not_Yet_Submitted()
    {
        var draft = CreateCompleteDraft();

        var result = draft.ConvertToEmployee(EmployeeCode.Create("EMP-200").Value, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("onboarding_draft.not_submitted");
    }

    [Fact]
    public void ConvertToEmployee_Should_Produce_An_Employee_Preserving_Document_And_Consent_State()
    {
        var draft = CreateCompleteDraft();
        var document = draft.Documents.Single();
        document.MarkScanned(DocumentScanStatus.Clean);
        document.RecordOcrSuggestion("{\"firstName\":\"Ocr Guess\"}", 0.42);
        draft.Submit(Now, "hr@vespera.test");

        var result = draft.ConvertToEmployee(EmployeeCode.Create("EMP-200").Value, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        var employee = result.Value;
        employee.Documents.Should().ContainSingle();
        employee.Documents.Single().Id.Should().Be(document.Id);
        employee.Documents.Single().ScanStatus.Should().Be(DocumentScanStatus.Clean);
        employee.Documents.Single().OcrSuggestedFieldsJson.Should().Be("{\"firstName\":\"Ocr Guess\"}");
        employee.ConsentRecords.Should().ContainSingle(c => c.ConsentType == ConsentType.DataProcessing);
        draft.Status.Should().Be(OnboardingStatus.Converted);
        draft.ConvertedEmployeeId.Should().Be(employee.Id);
    }

    [Fact]
    public void OnboardingDraftId_Instances_With_The_Same_Value_Should_Be_Equal()
    {
        var value = Guid.NewGuid();

        new OnboardingDraftId(value).Should().Be(new OnboardingDraftId(value));
        OnboardingDraftId.New().Should().NotBe(OnboardingDraftId.New());
    }

    private static OnboardingDraft CreateDraft() =>
        OnboardingDraft.StartDraft(TenantId, Now, "hr@vespera.test").Value;

    private static OnboardingDraft CreateDraftThroughEmploymentDetails()
    {
        var draft = CreateDraft();
        draft.UpdatePersonalDetails("Ada", "Lovelace", Email, Phone, new DateOnly(1990, 1, 1), Now, "hr@vespera.test");
        draft.UpdateEmploymentDetails(DepartmentId.New(), DesignationId.New(), LocationId.New(), new DateOnly(2026, 2, 1), Now, "hr@vespera.test");
        return draft;
    }

    private static OnboardingDraft CreateCompleteDraft()
    {
        var draft = CreateDraftThroughEmploymentDetails();
        draft.AddDocument(EmployeeDocumentType.Id, "storage/id.pdf", Now);
        draft.RecordConsent(ConsentType.DataProcessing, Now);
        return draft;
    }
}
