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

public sealed class EmployeeDocument : Entity<EmployeeDocumentId>
{
    internal EmployeeDocument(EmployeeDocumentId id, EmployeeDocumentType documentType, string fileReference, DateTimeOffset uploadedAt)
        : base(id)
    {
        DocumentType = documentType;
        FileReference = fileReference;
        UploadedAt = uploadedAt;
        VerificationStatus = DocumentVerificationStatus.Pending;
    }

    public EmployeeDocumentType DocumentType { get; }

    public string FileReference { get; }

    public DateTimeOffset UploadedAt { get; }

    public DocumentVerificationStatus VerificationStatus { get; private set; }

    public string? RejectionReason { get; private set; }

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
