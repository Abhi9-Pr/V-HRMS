using System.Runtime.CompilerServices;
using FluentAssertions;

namespace Vespera.Architecture.Tests;

/// <summary>
/// Enforces item 2's "documented, explicit escape hatch usable only from designated admin
/// queries": <c>IgnoreQueryFilters</c> may appear in exactly one file. NetArchTest operates on
/// type/namespace-level dependencies, not method-body call sites, so this is a source-scan rather
/// than an IL-based rule — still fails the build the moment a second call site appears anywhere
/// in Vespera.Infrastructure.
/// </summary>
public class QueryFilterEscapeHatchTests
{
    private const string AllowedFileName = "ReadRepositoryAdmin.cs";

    [Fact]
    public void IgnoreQueryFilters_Should_Only_Be_Called_From_ReadRepositoryAdmin()
    {
        var infrastructureRoot = GetInfrastructureProjectPath();

        var offendingFiles = Directory.EnumerateFiles(infrastructureRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => Path.GetFileName(path) != AllowedFileName)
            .Where(path => File.ReadAllText(path).Contains("IgnoreQueryFilters", StringComparison.Ordinal))
            .ToList();

        offendingFiles.Should().BeEmpty(
            "IgnoreQueryFilters is the documented tenant/soft-delete escape hatch and must only be called from {0}, but also appears in: {1}",
            AllowedFileName, string.Join(", ", offendingFiles));
    }

    private static string GetInfrastructureProjectPath([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", "..", "Vespera.Infrastructure"));
}
