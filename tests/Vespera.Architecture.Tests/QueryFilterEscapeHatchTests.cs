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

    // [CallerFilePath] is a compile-time constant, which the .NET SDK rewrites to a deterministic
    // "/_/..." placeholder instead of the real checkout path whenever ContinuousIntegrationBuild
    // is enabled (Directory.Build.props turns that on whenever CI=true, which every CI run sets) —
    // so it must not be used to locate real files at runtime. Walking up from the actual runtime
    // output directory to the repo root (marked by Vespera.sln) works in every environment.
    private static string GetInfrastructureProjectPath() => Path.Combine(RepositoryRoot.Value, "Vespera.Infrastructure");

    private static readonly Lazy<string> RepositoryRoot = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Vespera.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException($"Could not locate Vespera.sln above {AppContext.BaseDirectory}.");
    });
}
