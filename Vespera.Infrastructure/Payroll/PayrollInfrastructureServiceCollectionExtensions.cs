using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Payroll.BankFiles;

namespace Vespera.Infrastructure.Payroll;

public static class PayrollInfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the payslip renderer, password protector, and every
    /// <see cref="IBankFileFormatter"/> — adding a bank means adding one more
    /// <c>services.AddSingleton&lt;IBankFileFormatter, ...&gt;()</c> line here, nothing else.</summary>
    public static IServiceCollection AddVesperaPayroll(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddSingleton<IPayslipRenderer, QuestPdfPayslipRenderer>();
        services.AddSingleton<IPdfPasswordProtector, PdfSharpPasswordProtector>();

        services.AddSingleton<IBankFileFormatter, IciciBankFileFormatter>();
        services.AddSingleton<IBankFileFormatter, HdfcBankFileFormatter>();
        services.AddSingleton<IBankFileFormatter, SbiBankFileFormatter>();

        return services;
    }
}
