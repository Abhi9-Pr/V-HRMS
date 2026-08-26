using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Services;
using Vespera.Infrastructure.Payroll;

namespace Vespera.Architecture.Tests;

/// <summary>
/// Mechanically proves the brief's core OCP requirement: adding a payroll component (or a bank
/// file format) requires exactly one new class and one registration line, nothing else. Reflection
/// finds every concrete implementation of the extension point; a real <see cref="IServiceCollection"/>
/// built the same way the app builds it proves each one is actually wired up — not just that the
/// interface exists, but that the composition root really resolves it.
/// </summary>
public class PayrollRulePipelineArchitectureTests
{
    [Fact]
    public void Every_IPayrollComponentRule_Implementation_Should_Be_Registered_In_DI()
    {
        var implementations = typeof(AssemblyReference).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IPayrollComponentRule).IsAssignableFrom(type))
            .ToList();

        implementations.Should().NotBeEmpty("the payroll rules pipeline should have at least its nine documented stages");

        var services = new ServiceCollection();
        services.AddApplication();

        var registeredTypes = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPayrollComponentRule))
            .Select(descriptor => descriptor.ImplementationType)
            .ToList();

        var unregistered = implementations.Where(type => !registeredTypes.Contains(type)).Select(type => type.FullName).ToList();

        unregistered.Should().BeEmpty(
            "every IPayrollComponentRule implementation must be registered in ApplicationServiceCollectionExtensions.AddApplication, but found unregistered: {0}",
            string.Join(", ", unregistered));
    }

    [Fact]
    public void Every_Registered_IPayrollComponentRule_Should_Have_A_Unique_Order()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        var rules = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPayrollComponentRule) && descriptor.ImplementationType is not null)
            .Select(descriptor => (IPayrollComponentRule)Activator.CreateInstance(descriptor.ImplementationType!)!)
            .ToList();

        var orders = rules.Select(rule => rule.Order).ToList();

        orders.Should().OnlyHaveUniqueItems("two stages sharing an Order would make pipeline execution order ambiguous");
    }

    [Fact]
    public void Every_IBankFileFormatter_Implementation_Should_Be_Registered_In_DI()
    {
        var implementations = typeof(QuestPdfPayslipRenderer).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IBankFileFormatter).IsAssignableFrom(type))
            .ToList();

        implementations.Should().NotBeEmpty("ICICI/HDFC/SBI formatters should all be present");

        var services = new ServiceCollection();
        services.AddVesperaPayroll();

        var registeredTypes = services
            .Where(descriptor => descriptor.ServiceType == typeof(IBankFileFormatter))
            .Select(descriptor => descriptor.ImplementationType)
            .ToList();

        var unregistered = implementations.Where(type => !registeredTypes.Contains(type)).Select(type => type.FullName).ToList();

        unregistered.Should().BeEmpty(
            "every IBankFileFormatter implementation must be registered in AddVesperaPayroll, but found unregistered: {0}",
            string.Join(", ", unregistered));
    }

    [Fact]
    public void Every_Payroll_Query_Handler_Should_Depend_On_The_Read_Auditor()
    {
        var handlerTypes = typeof(AssemblyReference).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Namespace == "Vespera.Application.Features.Payroll")
            .Where(type => type.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
            .ToList();

        handlerTypes.Should().NotBeEmpty("the payroll feature should have at least its run/variance query handlers");

        var offenders = handlerTypes
            .Where(handlerType => !ConstructorTakesAuditor(handlerType))
            .Select(handlerType => handlerType.FullName)
            .ToList();

        offenders.Should().BeEmpty(
            "every Payroll query handler must depend on IPiiAccessAuditor and call RecordAccessAsync (audit-on-read), but found handlers missing it: {0}",
            string.Join(", ", offenders));
    }

    private static bool ConstructorTakesAuditor(Type handlerType) =>
        handlerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Any(ctor => ctor.GetParameters().Any(parameter => parameter.ParameterType == typeof(IPiiAccessAuditor)));
}
