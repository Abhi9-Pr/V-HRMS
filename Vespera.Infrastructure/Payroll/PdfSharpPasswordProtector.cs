using PdfSharpCore.Pdf.IO;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Payroll;

/// <summary>
/// Applies an "open" (user) password to an already-rendered PDF by reloading it with PdfSharpCore
/// and re-saving with <c>SecuritySettings</c> populated. See <see cref="IPdfPasswordProtector"/>'s
/// remarks: this is the PAN+DDMM obfuscation scheme, not the real access control mechanism.
/// </summary>
public sealed class PdfSharpPasswordProtector : IPdfPasswordProtector
{
    public byte[] Protect(byte[] pdfBytes, string password)
    {
        using var inputStream = new MemoryStream(pdfBytes);
        using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

        document.SecuritySettings.UserPassword = password;
        document.SecuritySettings.OwnerPassword = password;
        document.SecuritySettings.PermitPrint = true;
        document.SecuritySettings.PermitModifyDocument = false;
        document.SecuritySettings.PermitExtractContent = false;
        document.SecuritySettings.PermitAnnotations = false;

        using var outputStream = new MemoryStream();
        document.Save(outputStream);
        return outputStream.ToArray();
    }
}
