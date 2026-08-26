using Vespera.Domain.Common;
using Vespera.Domain.Eis.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Eis;

public readonly record struct EmployeeId(Guid Value)
{
    public static EmployeeId New() => new(Guid.NewGuid());
}

public enum EmploymentStatus
{
    Active,
    OnLeave,
    Suspended,
    Exited,
}

public enum EmployeeExitReason
{
    Resignation,
    Termination,
    Retirement,
    EndOfContract,
}

/// <summary>Self-reported, optional. Only consumed by gender-restricted <see cref="Leave.LeaveType"/>
/// eligibility rules (e.g. maternity/paternity leave) — never required to onboard an employee.</summary>
public enum Gender
{
    Female,
    Male,
    Other,
}

public sealed class Employee : AuditableTenantAggregateRoot<EmployeeId>
{
    private readonly List<EmploymentHistory> _employmentHistory = [];
    private readonly List<EmployeeDocument> _documents = [];
    private readonly List<ConsentRecord> _consentRecords = [];

    private Employee(
        EmployeeId id, TenantId tenantId, EmployeeCode code, string firstName, string lastName,
        EmailAddress workEmail, PhoneNumber phone, DateOnly dateOfBirth, DateOnly dateOfJoining,
        DepartmentId departmentId, DesignationId designationId, LocationId locationId,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Code = code;
        FirstName = firstName;
        LastName = lastName;
        WorkEmail = workEmail;
        Phone = phone;
        DateOfBirth = dateOfBirth;
        DateOfJoining = dateOfJoining;
        DepartmentId = departmentId;
        DesignationId = designationId;
        LocationId = locationId;
        Status = EmploymentStatus.Active;

        _employmentHistory.Add(new EmploymentHistory(
            EmploymentHistoryId.New(), departmentId, designationId, locationId, dateOfJoining, EmploymentChangeReason.Hire));
    }

    public EmployeeCode Code { get; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public EmailAddress WorkEmail { get; private set; }

    public PhoneNumber Phone { get; private set; }

    public DateOnly DateOfBirth { get; }

    public DateOnly DateOfJoining { get; }

    public DepartmentId DepartmentId { get; private set; }

    public DesignationId DesignationId { get; private set; }

    public LocationId LocationId { get; private set; }

    public EmploymentStatus Status { get; private set; }

    public Gender? Gender { get; private set; }

    public PanNumber? Pan { get; private set; }

    public BankAccountNumber? BankAccount { get; private set; }

    public DateOnly? ExitDate { get; private set; }

    public EmployeeExitReason? ExitReason { get; private set; }

    public Money? CurrentAnnualCtc { get; private set; }

    /// <summary>The raw user id configured on a biometric device for this employee — not
    /// necessarily related to any other identifier this system assigns. Null until an HR admin
    /// maps the device-side id to this employee, either up front or retroactively via a
    /// <c>QuarantinedBiometricPunch</c>.</summary>
    public string? BiometricDeviceUserId { get; private set; }

    public IReadOnlyCollection<EmploymentHistory> EmploymentHistory => _employmentHistory.AsReadOnly();

    public IReadOnlyCollection<EmployeeDocument> Documents => _documents.AsReadOnly();

    public IReadOnlyCollection<ConsentRecord> ConsentRecords => _consentRecords.AsReadOnly();

    public static Result<Employee> Onboard(
        TenantId tenantId, EmployeeCode code, string firstName, string lastName, EmailAddress workEmail, PhoneNumber phone,
        DateOnly dateOfBirth, DateOnly dateOfJoining, DepartmentId departmentId, DesignationId designationId, LocationId locationId,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result.Failure<Employee>(Error.Validation("employee.first_name_required", "First name is required."));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Failure<Employee>(Error.Validation("employee.last_name_required", "Last name is required."));
        }

        var employee = new Employee(
            EmployeeId.New(), tenantId, code, firstName.Trim(), lastName.Trim(), workEmail, phone,
            dateOfBirth, dateOfJoining, departmentId, designationId, locationId, occurredOn, createdBy);

        employee.Raise(new EmployeeOnboarded(employee.Id, tenantId, dateOfJoining, occurredOn));
        return Result.Success(employee);
    }

    public Result Transfer(
        DepartmentId departmentId, DesignationId designationId, LocationId locationId, DateOnly effectiveDate,
        EmploymentChangeReason reason, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == EmploymentStatus.Exited)
        {
            return Result.Failure(Error.Conflict("employee.exited", "Cannot transfer an exited employee."));
        }

        DepartmentId = departmentId;
        DesignationId = designationId;
        LocationId = locationId;
        _employmentHistory.Add(new EmploymentHistory(
            EmploymentHistoryId.New(), departmentId, designationId, locationId, effectiveDate, reason));
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Exit(DateOnly exitDate, EmployeeExitReason reason, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == EmploymentStatus.Exited)
        {
            return Result.Failure(Error.Conflict("employee.already_exited", "Employee has already exited."));
        }

        Status = EmploymentStatus.Exited;
        ExitDate = exitDate;
        ExitReason = reason;
        Touch(occurredOn, modifiedBy);
        Raise(new EmployeeExited(Id, TenantId, exitDate, occurredOn));
        return Result.Success();
    }

    public Result UpdateStatutoryDetails(PanNumber? pan, BankAccountNumber? bankAccount, DateTimeOffset occurredOn, string modifiedBy)
    {
        Pan = pan;
        BankAccount = bankAccount;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result UpdateCompensation(Money? annualCtc, DateTimeOffset occurredOn, string modifiedBy)
    {
        CurrentAnnualCtc = annualCtc;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result UpdatePersonalDetails(
        string firstName, string lastName, EmailAddress workEmail, PhoneNumber phone, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result.Failure(Error.Validation("employee.first_name_required", "First name is required."));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Failure(Error.Validation("employee.last_name_required", "Last name is required."));
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        WorkEmail = workEmail;
        Phone = phone;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result SetGender(Gender? gender, DateTimeOffset occurredOn, string modifiedBy)
    {
        Gender = gender;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result AssignBiometricDeviceUserId(string? deviceUserId, DateTimeOffset occurredOn, string modifiedBy)
    {
        BiometricDeviceUserId = string.IsNullOrWhiteSpace(deviceUserId) ? null : deviceUserId.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public EmployeeDocument AddDocument(EmployeeDocumentType documentType, string fileReference, DateTimeOffset uploadedAt)
    {
        var document = new EmployeeDocument(EmployeeDocumentId.New(), documentType, fileReference, uploadedAt);
        _documents.Add(document);
        return document;
    }

    public Result RemoveDocument(EmployeeDocumentId documentId)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId);
        if (document is null)
        {
            return Result.Failure(Error.NotFound("employee_document.not_found", "Document not found."));
        }

        _documents.Remove(document);
        return Result.Success();
    }

    public ConsentRecord RecordConsent(ConsentType consentType, DateTimeOffset grantedAt)
    {
        var consent = new ConsentRecord(ConsentRecordId.New(), consentType, grantedAt);
        _consentRecords.Add(consent);
        return consent;
    }

    /// <summary>Hands an <see cref="OnboardingDraft"/>'s already-built documents/consents to the
    /// newly-converted employee, preserving whatever scan/OCR/verification state they
    /// accumulated during the draft phase rather than re-creating them from scratch. Only
    /// <see cref="OnboardingDraft.ConvertToEmployee"/> calls this.</summary>
    internal void AttachOnboardingArtifacts(IReadOnlyCollection<EmployeeDocument> documents, IReadOnlyCollection<ConsentRecord> consentRecords)
    {
        _documents.AddRange(documents);
        _consentRecords.AddRange(consentRecords);
    }
}
