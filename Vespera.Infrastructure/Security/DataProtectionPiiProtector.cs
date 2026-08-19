using Microsoft.AspNetCore.DataProtection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Security;

/// <summary>
/// Encrypts PII columns at rest (e.g. <c>Employee.Pan</c>, <c>Employee.BankAccount</c>) using
/// ASP.NET Core Data Protection. Chosen over column-level database encryption because
/// <see cref="IPiiProtector"/>'s string-to-string shape already matches <see cref="IDataProtector"/>
/// exactly, needs no schema/column-type changes, and behaves identically across every supported
/// database engine (Postgres, SQL Server, SQLite) since the encryption happens in the app, not the DB.
/// </summary>
public sealed class DataProtectionPiiProtector : IPiiProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionPiiProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Vespera.Pii.v1");
    }

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
