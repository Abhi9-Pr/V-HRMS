using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct PayslipId(Guid Value)
{
    public static PayslipId New() => new(Guid.NewGuid());
}

public sealed class Payslip : AggregateRoot<PayslipId>, ITenantScoped
{
    private Payslip(PayslipId id, TenantId tenantId, PayrollRunId payrollRunId, EmployeeId employeeId, Money netPay, DateTimeOffset generatedAt)
        : base(id)
    {
        TenantId = tenantId;
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        NetPay = netPay;
        GeneratedAt = generatedAt;
        IsPublished = false;
    }

    public TenantId TenantId { get; }

    public PayrollRunId PayrollRunId { get; }

    public EmployeeId EmployeeId { get; }

    public Money NetPay { get; }

    public DateTimeOffset GeneratedAt { get; }

    public bool IsPublished { get; private set; }

    public static Payslip Generate(TenantId tenantId, PayrollRunId payrollRunId, EmployeeId employeeId, Money netPay, DateTimeOffset generatedAt) =>
        new(PayslipId.New(), tenantId, payrollRunId, employeeId, netPay, generatedAt);

    public Result MarkPublished()
    {
        if (IsPublished)
        {
            return Result.Failure(Error.Conflict("payslip.already_published", "Payslip is already published."));
        }

        IsPublished = true;
        return Result.Success();
    }
}
