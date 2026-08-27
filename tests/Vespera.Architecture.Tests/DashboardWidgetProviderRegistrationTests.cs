using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Architecture.Tests;

/// <summary>
/// Mechanically proves the landing dashboard's OCP requirement: adding a widget requires exactly
/// one new <see cref="IDashboardWidgetProvider"/> class and one registration line, nothing else —
/// the same shape as <c>PayrollRulePipelineArchitectureTests</c> for the payroll rules pipeline.
/// </summary>
public class DashboardWidgetProviderRegistrationTests
{
    [Fact]
    public void Every_IDashboardWidgetProvider_Implementation_Should_Be_Registered_In_DI()
    {
        var implementations = typeof(AssemblyReference).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDashboardWidgetProvider).IsAssignableFrom(type))
            .ToList();

        implementations.Should().NotBeEmpty("the landing dashboard should ship with at least its documented widgets");

        var services = new ServiceCollection();
        services.AddApplication();

        var registeredTypes = services
            .Where(descriptor => descriptor.ServiceType == typeof(IDashboardWidgetProvider))
            .Select(descriptor => descriptor.ImplementationType)
            .ToList();

        var unregistered = implementations.Where(type => !registeredTypes.Contains(type)).Select(type => type.FullName).ToList();

        unregistered.Should().BeEmpty(
            "every IDashboardWidgetProvider implementation must be registered in ApplicationServiceCollectionExtensions.AddApplication, but found unregistered: {0}",
            string.Join(", ", unregistered));
    }

    [Fact]
    public void Every_Registered_IDashboardWidgetProvider_Should_Have_A_Unique_WidgetKey()
    {
        // WidgetKey/DefaultOrder/DefaultVisible/DefaultSize are pure expression-bodied properties
        // on every provider (no constructor-built state involved), so an uninitialized instance
        // (constructor skipped — providers take repository/service dependencies this test has no
        // DI graph to satisfy) reads them safely.
        var services = new ServiceCollection();
        services.AddApplication();

        var keys = services
            .Where(descriptor => descriptor.ServiceType == typeof(IDashboardWidgetProvider) && descriptor.ImplementationType is not null)
            .Select(descriptor => (IDashboardWidgetProvider)RuntimeHelpers.GetUninitializedObject(descriptor.ImplementationType!))
            .Select(provider => provider.WidgetKey)
            .ToList();

        keys.Should().OnlyHaveUniqueItems("two widgets sharing a WidgetKey would collide in a saved DashboardLayout");
    }
}
