using Vespera.Domain.Common;

namespace Vespera.Domain.Eis;

public readonly record struct EmployeeDocumentId(Guid Value)
{
    public static EmployeeDocumentId New() => new(Guid.NewGuid());
}

public enum EmployeeDocumentType
{
    Id,
    Contract,
    Certificate,
    Other,
}

public enum DocumentVerificationStatus
{
    Pending,
    Verified,
    Rejected,
}

public enum DocumentScanStatus
{
    Pending,
    Clean,
    Infected,
    Failed,
}

public sealed class EmployeeDocument : Entity<EmployeeDocumentId>
{
    internal EmployeeDocument(EmployeeDocumentId id, EmployeeDocumentType documentType, string fileReference, DateTimeOffset uploadedAt)
        : base(id)
    {
        DocumentType = documentType;
        FileReference = fileReference;
        UploadedAt = uploadedAt;
        VerificationStatus = DocumentVerificationStatus.Pending;
        ScanStatus = DocumentScanStatus.Pending;
    }

    public EmployeeDocumentType DocumentType { get; }

    public string FileReference { get; }

    public DateTimeOffset UploadedAt { get; }

    public DocumentVerificationStatus VerificationStatus { get; private set; }

    public string? RejectionReason { get; private set; }

    public DocumentScanStatus ScanStatus { get; private set; }

    /// <summary>Raw OCR output for this document — a suggestion only. Never applied to
    /// <see cref="Employee"/> until a human explicitly confirms it via <see cref="ConfirmOcrSuggestion"/>.</summary>
    public string? OcrSuggestedFieldsJson { get; private set; }

    public double? OcrConfidence { get; private set; }

    public bool IsOcrConfirmed { get; private set; }

    /// <summary>What the human actually confirmed — may differ from <see cref="OcrSuggestedFieldsJson"/>.</summary>
    public string? ConfirmedFieldsJson { get; private set; }

    public string? ConfirmedBy { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public Result MarkScanned(DocumentScanStatus status)
    {
        ScanStatus = status;
        return Result.Success();
    }

    public Result RecordOcrSuggestion(string fieldsJson, double confidence)
    {
        if (string.IsNullOrWhiteSpace(fieldsJson))
        {
            return Result.Failure(Error.Validation("employee_document.ocr_fields_required", "OCR fields payload is required."));
        }

        if (confidence is < 0 or > 1)
        {
            return Result.Failure(Error.Validation("employee_document.ocr_confidence_out_of_range", "OCR confidence must be between 0 and 1."));
        }

        OcrSuggestedFieldsJson = fieldsJson;
        OcrConfidence = confidence;
        return Result.Success();
    }

    public Result ConfirmOcrSuggestion(string confirmedFieldsJson, string confirmedBy, DateTimeOffset confirmedAt)
    {
        if (string.IsNullOrWhiteSpace(confirmedFieldsJson))
        {
            return Result.Failure(Error.Validation("employee_document.confirmed_fields_required", "Confirmed fields payload is required."));
        }

        ConfirmedFieldsJson = confirmedFieldsJson;
        ConfirmedBy = confirmedBy;
        ConfirmedAt = confirmedAt;
        IsOcrConfirmed = true;
        return Result.Success();
    }

    public Result Verify()
    {
        if (VerificationStatus == DocumentVerificationStatus.Verified)
        {
            return Result.Failure(Error.Conflict("employee_document.already_verified", "Document is already verified."));
        }

        VerificationStatus = DocumentVerificationStatus.Verified;
        RejectionReason = null;
        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("employee_document.rejection_reason_required", "Rejection reason is required."));
        }

        VerificationStatus = DocumentVerificationStatus.Rejected;
        RejectionReason = reason.Trim();
        return Result.Success();
    }
}
