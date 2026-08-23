using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Eis;

public readonly record struct OnboardingDraftId(Guid Value)
{
    public static OnboardingDraftId New() => new(Guid.NewGuid());
}

/// <summary>Which step a draft has most recently completed. Advances forward only — revisiting
/// an earlier step's update method never regresses it, so the wizard UI can always trust
/// <see cref="OnboardingDraft.CurrentStep"/> as "furthest step reached," not "step currently open."</summary>
public enum OnboardingStep
{
    PersonalDetails,
    EmploymentDetails,
    Documents,
    Consent,
    Review,
}

public enum OnboardingStatus
{
    Draft,
    Submitted,
    Converted,
}

/// <summary>
/// A multi-step, partially-completable onboarding record for a not-yet-hired person. Holds
/// <see cref="EmployeeDocument"/>/<see cref="ConsentRecord"/> instances directly — both have
/// `internal` constructors, which in C# scopes to the whole <c>Vespera.Domain</c> assembly, not
/// just <see cref="Employee"/>, so this aggregate can build them itself rather than inventing
/// parallel "draft document"/"draft consent" types. <see cref="ConvertToEmployee"/> hands those
/// same instances to the new <see cref="Employee"/> via <see cref="Employee.AttachOnboardingArtifacts"/>,
/// preserving whatever scan/OCR/verification state they already accumulated during the draft
/// phase instead of re-creating them from scratch.
/// </summary>
public sealed class OnboardingDraft : AuditableTenantAggregateRoot<OnboardingDraftId>
{
    private readonly List<EmployeeDocument> _documents = [];
    private readonly List<ConsentRecord> _consentRecords = [];

    private OnboardingDraft(OnboardingDraftId id, TenantId tenantId, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Status = OnboardingStatus.Draft;
        CurrentStep = OnboardingStep.PersonalDetails;
    }

    public OnboardingStatus Status { get; private set; }

    public OnboardingStep CurrentStep { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public EmailAddress? WorkEmail { get; private set; }

    public PhoneNumber? Phone { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }

    public DepartmentId? DepartmentId { get; private set; }

    public DesignationId? DesignationId { get; private set; }

    public LocationId? LocationId { get; private set; }

    public DateOnly? DateOfJoining { get; private set; }

    /// <summary>Set once <see cref="ConvertToEmployee"/> succeeds — traceability from draft to
    /// the employee it became.</summary>
    public EmployeeId? ConvertedEmployeeId { get; private set; }

    public IReadOnlyCollection<EmployeeDocument> Documents => _documents.AsReadOnly();

    public IReadOnlyCollection<ConsentRecord> ConsentRecords => _consentRecords.AsReadOnly();

    public static Result<OnboardingDraft> StartDraft(TenantId tenantId, DateTimeOffset createdAt, string createdBy)
    {
        var draft = new OnboardingDraft(OnboardingDraftId.New(), tenantId, createdAt, createdBy);
        return Result.Success(draft);
    }

    public Result UpdatePersonalDetails(
        string firstName, string lastName, EmailAddress workEmail, PhoneNumber phone, DateOnly dateOfBirth,
        DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result.Failure(Error.Validation("onboarding_draft.first_name_required", "First name is required."));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Failure(Error.Validation("onboarding_draft.last_name_required", "Last name is required."));
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        WorkEmail = workEmail;
        Phone = phone;
        DateOfBirth = dateOfBirth;

        if (CurrentStep < OnboardingStep.EmploymentDetails)
        {
            CurrentStep = OnboardingStep.EmploymentDetails;
        }

        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result UpdateEmploymentDetails(
        DepartmentId departmentId, DesignationId designationId, LocationId locationId, DateOnly dateOfJoining,
        DateTimeOffset occurredOn, string modifiedBy)
    {
        DepartmentId = departmentId;
        DesignationId = designationId;
        LocationId = locationId;
        DateOfJoining = dateOfJoining;

        if (CurrentStep < OnboardingStep.Documents)
        {
            CurrentStep = OnboardingStep.Documents;
        }

        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public EmployeeDocument AddDocument(EmployeeDocumentType documentType, string fileReference, DateTimeOffset uploadedAt)
    {
        var document = new EmployeeDocument(EmployeeDocumentId.New(), documentType, fileReference, uploadedAt);
        _documents.Add(document);
        return document;
    }

    public ConsentRecord RecordConsent(ConsentType consentType, DateTimeOffset grantedAt)
    {
        var consent = new ConsentRecord(ConsentRecordId.New(), consentType, grantedAt);
        _consentRecords.Add(consent);
        return consent;
    }

    /// <summary>Checks the actual completeness conditions directly rather than trusting
    /// <see cref="CurrentStep"/> — a draft that jumped straight to uploading a document without
    /// ever calling <see cref="UpdateEmploymentDetails"/> should still be rejected here, not
    /// waved through because some step-tracking field happens to read "far enough."</summary>
    public Result Submit(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status != OnboardingStatus.Draft)
        {
            return Result.Failure(Error.Conflict(
                "onboarding_draft.not_in_draft", "This draft has already been submitted or converted."));
        }

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName)
            || WorkEmail is null || Phone is null || DateOfBirth is null)
        {
            return Result.Failure(Error.Validation(
                "onboarding_draft.personal_details_incomplete", "Personal details are incomplete."));
        }

        if (DepartmentId is null || DesignationId is null || LocationId is null || DateOfJoining is null)
        {
            return Result.Failure(Error.Validation(
                "onboarding_draft.employment_details_incomplete", "Employment details are incomplete."));
        }

        if (!_consentRecords.Any(consent => consent.ConsentType == ConsentType.DataProcessing && consent.Granted))
        {
            return Result.Failure(Error.Validation(
                "onboarding_draft.consent_required", "Data-processing consent has not been recorded."));
        }

        if (!_documents.Any(document => document.DocumentType == EmployeeDocumentType.Id))
        {
            return Result.Failure(Error.Validation(
                "onboarding_draft.id_document_required", "At least one ID document is required."));
        }

        Status = OnboardingStatus.Submitted;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result<Employee> ConvertToEmployee(EmployeeCode employeeCode, DateTimeOffset occurredOn, string convertedBy)
    {
        if (Status != OnboardingStatus.Submitted)
        {
            return Result.Failure<Employee>(Error.Conflict(
                "onboarding_draft.not_submitted", "This draft has not been submitted yet."));
        }

        var employeeResult = Employee.Onboard(
            TenantId, employeeCode, FirstName!, LastName!, WorkEmail!, Phone!,
            DateOfBirth!.Value, DateOfJoining!.Value, DepartmentId!.Value, DesignationId!.Value, LocationId!.Value,
            occurredOn, convertedBy);

        if (employeeResult.IsFailure)
        {
            return employeeResult;
        }

        var employee = employeeResult.Value;
        employee.AttachOnboardingArtifacts(_documents, _consentRecords);

        Status = OnboardingStatus.Converted;
        ConvertedEmployeeId = employee.Id;
        Touch(occurredOn, convertedBy);

        return Result.Success(employee);
    }
}
