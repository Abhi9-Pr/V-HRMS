using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.Payroll.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class PayslipTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AttachDocument_Should_Fail_With_A_Blank_Storage_Key()
    {
        var payslip = Payslip.Generate(TenantId.New(), PayrollRunId.New(), EmployeeId.New(), Money.Of(45000m, Currency.Inr), [], Now);

        var result = payslip.AttachDocument(" ", "hash", Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AttachDocument_Should_Record_The_Storage_Key_And_Hash_And_Raise_An_Event()
    {
        var payslip = Payslip.Generate(TenantId.New(), PayrollRunId.New(), EmployeeId.New(), Money.Of(45000m, Currency.Inr), [], Now);

        var result = payslip.AttachDocument("payslips/2026-05/emp-1.pdf", "abc123", Now);

        result.IsSuccess.Should().BeTrue();
        payslip.StorageKey.Should().Be("payslips/2026-05/emp-1.pdf");
        payslip.DocumentHash.Should().Be("abc123");
        payslip.DomainEvents.Should().ContainSingle(e => e is PayslipDocumentAttached);
    }

    [Fact]
    public void MarkPublished_Should_Fail_When_Already_Published()
    {
        var payslip = Payslip.Generate(TenantId.New(), PayrollRunId.New(), EmployeeId.New(), Money.Of(45000m, Currency.Inr), [], Now);
        payslip.MarkPublished();

        var result = payslip.MarkPublished();

        result.IsFailure.Should().BeTrue();
    }
}
