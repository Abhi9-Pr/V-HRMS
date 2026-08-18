using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NetArchTest.Rules;

namespace Vespera.Architecture.Tests;

public class DependencyRuleTests
{
    private static readonly Assembly DomainAssembly = typeof(Vespera.Domain.Common.Result).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Vespera.Application.AssemblyReference).Assembly;

    private static readonly string[] BclAssemblyPrefixes =
    {
        "System",
        "netstandard",
        "mscorlib",
    };

    [Fact]
    public void Domain_Should_Not_Reference_Other_Vespera_Projects()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny("Vespera.Application", "Vespera.Infrastructure", "Vespera.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Domain_Should_Not_Reference_Any_ThirdParty_Package()
    {
        var offendingReferences = DomainAssembly.GetReferencedAssemblies()
            .Where(referenced => !IsBcl(referenced))
            .Select(referenced => referenced.Name)
            .ToList();

        offendingReferences.Should().BeEmpty(
            "Vespera.Domain must depend on nothing but the BCL, but references: {0}",
            string.Join(", ", offendingReferences));
    }

    [Fact]
    public void Domain_Csproj_Should_Not_Declare_Any_PackageReference()
    {
        // Belt-and-braces: a reflection-based check only sees packages a type actually uses.
        // An unused <PackageReference> would slip past it, so also inspect the project file itself.
        var csprojPath = GetDomainCsprojPath();

        File.Exists(csprojPath).Should().BeTrue($"expected to find {csprojPath}");
        File.ReadAllText(csprojPath).Should().NotContain(
            "PackageReference",
            "Vespera.Domain must declare zero third-party package references");
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny("Vespera.Infrastructure", "Vespera.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    private static string GetDomainCsprojPath([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(sourceFile)!, "..", "..", "Vespera.Domain", "Vespera.Domain.csproj"));

    private static bool IsBcl(AssemblyName assemblyName) =>
        BclAssemblyPrefixes.Any(prefix =>
            assemblyName.Name!.Equals(prefix, StringComparison.Ordinal) ||
            assemblyName.Name!.StartsWith(prefix + ".", StringComparison.Ordinal)) ||
        assemblyName.Name == DomainAssembly.GetName().Name;

    internal static string FailureMessage(TestResult result) =>
        result.FailingTypes is null
            ? "Architecture rule violated."
            : $"Architecture rule violated by: {string.Join(", ", result.FailingTypes.Select(t => t.FullName))}";
}
