using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Behaviors;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Application.Features.Recruitment.Rules;

namespace Vespera.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = AssemblyReference.Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<PermissionResolver>();

        services.AddScoped<CurrentEmployeeResolver>();
        services.AddScoped<ExpensePolicyEvaluator>();
        services.AddScoped<IExpensePolicyRule, MaxAmountPerClaimRule>();
        services.AddScoped<IExpensePolicyRule, ReceiptRequiredAboveAmountRule>();
        services.AddScoped<StageTransitionEvaluator>();
        services.AddScoped<IStageTransitionRule, RequiresCompletedInterviewBeforeOfferStageRule>();

        // Order matters: outermost first. See docs/CONTRIBUTING-slices.md for the rationale.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantScopeBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}
