using System.Reflection;
using FluentAssertions;
using MediatR;

namespace Vespera.Architecture.Tests;

public class MediatRHandlerTests
{
    private static readonly Assembly[] AssembliesToScan =
    {
        typeof(Vespera.Application.AssemblyReference).Assembly,
        typeof(Vespera.Infrastructure.AssemblyReference).Assembly,
        typeof(Vespera.Api.Program).Assembly,
    };

    [Fact]
    public void RequestHandlers_Should_Expose_Exactly_One_Public_Method()
    {
        var offenders = new List<string>();

        foreach (var assembly in AssembliesToScan)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(IsRequestHandler);

            foreach (var handlerType in handlerTypes)
            {
                var publicMethodCount = handlerType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Count(method => !method.IsSpecialName);

                if (publicMethodCount > 1)
                {
                    offenders.Add($"{handlerType.FullName} exposes {publicMethodCount} public methods");
                }
            }
        }

        offenders.Should().BeEmpty(
            "a MediatR request handler must have a single reason to change: one public Handle method, but found: {0}",
            string.Join(", ", offenders));
    }

    private static bool IsRequestHandler(Type type) =>
        type.GetInterfaces().Any(candidateInterface =>
            candidateInterface.IsGenericType &&
            candidateInterface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
}
