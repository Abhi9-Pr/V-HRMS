using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Vespera.Architecture.Tests;

public class NamingConventionTests
{
    private static readonly Assembly[] SolutionAssemblies =
    {
        typeof(Vespera.Domain.Common.Result).Assembly,
        typeof(Vespera.Application.AssemblyReference).Assembly,
        typeof(Vespera.Infrastructure.AssemblyReference).Assembly,
        typeof(Vespera.Api.Program).Assembly,
    };

    [Theory]
    [InlineData("Manager")]
    [InlineData("Helper")]
    [InlineData("Utils")]
    public void No_Type_Should_Have_A_Banned_Suffix(string suffix)
    {
        var result = Types.InAssemblies(SolutionAssemblies)
            .Should()
            .NotHaveNameEndingWith(suffix)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DependencyRuleTests.FailureMessage(result));
    }
}
