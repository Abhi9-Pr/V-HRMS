using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct PayslipId(Guid Value)
{
    public static PayslipId New() => new(Guid.NewGuid());
}

public sealed class Payslip : AggregateRoot<PayslipId>, ITenantScoped
{
    private readonly List<PayrollComponentLine> _lines;

    private Payslip(
        PayslipId id, TenantId tenantId, PayrollRunId payrollRunId, EmployeeId employeeId, Money netPay,
        IReadOnlyList<PayrollComponentLine> lines, DateTimeOffset generatedAt)
        : base(id)
    {
        TenantId = tenantId;
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        NetPay = netPay;
        _lines = [.. lines];
        GeneratedAt = generatedAt;
        IsPublished = false;
    }

    public TenantId TenantId { get; }

    public PayrollRunId PayrollRunId { get; }

    public EmployeeId EmployeeId { get; }

    public Money NetPay { get; }

    public IReadOnlyList<PayrollComponentLine> Lines => _lines.AsReadOnly();

    public DateTimeOffset GeneratedAt { get; }

    public bool IsPublished { get; private set; }

    /// <summary>Where the rendered PDF was uploaded via <c>IFileStorage</c> — null until
    /// <see cref="AttachDocument"/> is called. Downloads go through a short-lived signed URL over
    /// this key, never a direct path (see /docs/security-notes.md).</summary>
    public string? StorageKey { get; private set; }

    /// <summary>SHA-256 of the rendered PDF bytes, for tamper detection — not for access control.</summary>
    public string? DocumentHash { get; private set; }

    public static Payslip Generate(
        TenantId tenantId, PayrollRunId payrollRunId, EmployeeId employeeId, Money netPay,
        IReadOnlyList<PayrollComponentLine> lines, DateTimeOffset generatedAt) =>
        new(PayslipId.New(), tenantId, payrollRunId, employeeId, netPay, lines, generatedAt);

    public Result MarkPublished()
    {
        if (IsPublished)
        {
            return Result.Failure(Error.Conflict("payslip.already_published", "Payslip is already published."));
        }

        IsPublished = true;
        return Result.Success();
    }

    /// <summary>Records where the rendered, (obfuscation-only) password-protected PDF was stored
    /// and its content hash — the tamper-detection value the brief asks for. Real download
    /// authorization is enforced separately, per request, via the signed URL that serves
    /// <see cref="StorageKey"/> (see /docs/security-notes.md).</summary>
    public Result AttachDocument(string storageKey, string documentHash, DateTimeOffset occurredOn)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || string.IsNullOrWhiteSpace(documentHash))
        {
            return Result.Failure(Error.Validation("payslip.document_reference_required", "Storage key and document hash are required."));
        }

        StorageKey = storageKey;
        DocumentHash = documentHash;
        Raise(new PayslipDocumentAttached(Id, TenantId, EmployeeId, PayrollRunId, occurredOn));
        return Result.Success();
    }
}
