using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Behaviors;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Leave;
using Vespera.Application.Features.Payroll;
using Vespera.Application.Features.Payroll.Rules;
using Vespera.Domain.Services;

namespace Vespera.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = AssemblyReference.Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<PermissionResolver>();
        services.AddScoped<RegularizationApproverResolver>();
        services.AddScoped<LeaveApprovalChainBuilder>();
        services.AddScoped<LeaveApprovalStepAuthorizer>();

        // Haversine today; Vincenty (or another IDistanceCalculator) can swap in later behind the
        // same registration line — see GeofenceEvaluator's callers, none of which know which one
        // is active.
        services.AddSingleton<IDistanceCalculator, HaversineDistanceCalculator>();

        // The payroll rules pipeline: adding a component means adding one new IPayrollComponentRule
        // class and one registration line here, nothing else — see PayrollComponentRuleRegistrationTests
        // in Vespera.Architecture.Tests for the mechanical proof.
        services.AddScoped<PayrollComputationEngine>();
        services.AddScoped<IPayrollComponentRule, EarningsRule>();
        services.AddScoped<IPayrollComponentRule, AttendanceLopRule>();
        services.AddScoped<IPayrollComponentRule, ProvidentFundRule>();
        services.AddScoped<IPayrollComponentRule, EmployeeStateInsuranceRule>();
        services.AddScoped<IPayrollComponentRule, ProfessionalTaxRule>();
        services.AddScoped<IPayrollComponentRule, IncomeTaxRule>();
        services.AddScoped<IPayrollComponentRule, VoluntaryDeductionsRule>();
        services.AddScoped<IPayrollComponentRule, ReimbursementsRule>();
        services.AddScoped<IPayrollComponentRule, NetPayRule>();

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
