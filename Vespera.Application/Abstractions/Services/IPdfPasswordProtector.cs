namespace Vespera.Application.Abstractions.Services;

/// <summary>
/// Applies the PAN+DDMM password scheme to a rendered payslip PDF (see <c>GeneratePayslipCommandHandler</c>
/// for how the password itself is derived). This is obfuscation, not access control: anyone with
/// the PDF bytes and enough patience can still brute-force or strip a PDF "open" password. Real
/// download authorization is the short-lived signed URL + server-side permission check on every
/// request (see <c>GetPayslipDownloadUrlQuery</c> and <c>/docs/security-notes.md</c>). Kept as its
/// own seam so the scheme — or whether to apply one at all — can change without touching the renderer.
/// </summary>
public interface IPdfPasswordProtector
{
    public byte[] Protect(byte[] pdfBytes, string password);
}
