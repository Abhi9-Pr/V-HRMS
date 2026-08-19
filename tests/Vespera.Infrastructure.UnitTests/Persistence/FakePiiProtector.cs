using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.UnitTests.Persistence;

/// <summary>Deterministic, reversible fake — real enough to prove a column is round-tripped
/// through *some* transform (never stored as the literal clear value) without pulling in
/// ASP.NET Core Data Protection for a unit test.</summary>
internal sealed class FakePiiProtector : IPiiProtector
{
    private const string Prefix = "enc:";

    public string Protect(string plainText) => Prefix + plainText;

    public string Unprotect(string protectedText) =>
        protectedText.StartsWith(Prefix, StringComparison.Ordinal) ? protectedText[Prefix.Length..] : protectedText;
}
