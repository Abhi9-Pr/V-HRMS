using FluentAssertions;

namespace Vespera.Architecture.Tests;

public class ControllerConstructorTests
{
    [Fact]
    public void Controllers_Should_Not_Depend_On_A_DbContext_In_Their_Constructor()
    {
        var apiAssembly = typeof(Vespera.Api.Program).Assembly;

        var controllerTypes = apiAssembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal));

        var offenders = controllerTypes
            .Where(controller => controller.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters())
                .Any(parameter => parameter.ParameterType.Name.EndsWith("DbContext", StringComparison.Ordinal)))
            .Select(controller => controller.FullName ?? controller.Name)
            .ToList();

        offenders.Should().BeEmpty(
            "controllers must send a MediatR message instead of depending on a DbContext directly, but found: {0}",
            string.Join(", ", offenders));
    }
}
