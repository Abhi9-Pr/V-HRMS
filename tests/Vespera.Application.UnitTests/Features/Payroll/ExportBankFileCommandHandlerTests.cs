using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class ExportBankFileCommandHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IBankFileFormatter _formatter = Substitute.For<IBankFileFormatter>();
    private readonly TenantId _tenantId = TenantId.New();

    private ExportBankFileCommandHandler CreateHandler() => new(_payrollRuns, _employees, [_formatter]);

    private static Employee OnboardWithBankAccount(TenantId tenantId, string code)
    {
        var employee = Employee.Onboard(
            tenantId, EmployeeCode.Create(code).Value, "Ada", "Lovelace",
            EmailAddress.Create($"{code.ToLowerInvariant()}@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            DateTimeOffset.UtcNow, "seed").Value;
        employee.UpdateStatutoryDetails(null, BankAccountNumber.Create("123456789012").Value, DateTimeOffset.UtcNow, "seed");
        return employee;
    }

    private PayrollRun FinalizedRunWithOneLine(EmployeeId employeeId, DateTimeOffset now)
    {
        var run = PayrollRun.Open(_tenantId, 5, 2026, now, "seed").Value;
        run.FreezeAttendance(new DateOnly(2026, 5, 26), 25, now, "seed");
        run.RecomputeLines(
            [new PayrollLineInput(employeeId, Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0)],
            "seed", now);
        run.SubmitForReview();
        run.Approve("seed", now);
        run.Finalize(now);
        return run;
    }

    [Fact]
    public async Task Handle_Should_Export_A_Bank_File_For_Employees_With_Bank_Details_On_A_Finalized_Run()
    {
        var now = DateTimeOffset.UtcNow;
        var employee = OnboardWithBankAccount(_tenantId, "EMP-200");
        var run = FinalizedRunWithOneLine(employee.Id, now);

        _formatter.BankCode.Returns("HDFC");
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);
        _employees.FirstOrDefaultAsync(Arg.Any<Vespera.Application.Features.Employees.EmployeeByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(employee);
        var exportResult = new BankFileExportResult("file.txt", "content", "report");
        _formatter.Format(Arg.Any<IReadOnlyList<BankTransferLine>>()).Returns(exportResult);

        var result = await CreateHandler().Handle(new ExportBankFileCommand(run.Id.Value, "HDFC"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(exportResult);
        _formatter.Received(1).Format(Arg.Is<IReadOnlyList<BankTransferLine>>(lines => lines.Count == 1));
    }

    [Fact]
    public async Task Handle_Should_Skip_Employees_With_No_Bank_Account_On_Record_And_Fail_If_None_Have_One()
    {
        var now = DateTimeOffset.UtcNow;
        var employee = Employee.Onboard(
            _tenantId, EmployeeCode.Create("EMP-201").Value, "No", "Bank",
            EmailAddress.Create("nobank@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            DateTimeOffset.UtcNow, "seed").Value;
        var run = FinalizedRunWithOneLine(employee.Id, now);

        _formatter.BankCode.Returns("HDFC");
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);
        _employees.FirstOrDefaultAsync(Arg.Any<Vespera.Application.Features.Employees.EmployeeByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await CreateHandler().Handle(new ExportBankFileCommand(run.Id.Value, "HDFC"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("bank_export.no_bank_details");
    }

    [Fact]
    public async Task Handle_Should_Fail_For_An_Unknown_Bank_Code()
    {
        _formatter.BankCode.Returns("HDFC");

        var result = await CreateHandler().Handle(new ExportBankFileCommand(Guid.NewGuid(), "ICICI"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("bank_export.unknown_bank");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Is_Not_Finalized_Or_Published()
    {
        var now = DateTimeOffset.UtcNow;
        var run = PayrollRun.Open(_tenantId, 5, 2026, now, "seed").Value;

        _formatter.BankCode.Returns("HDFC");
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);

        var result = await CreateHandler().Handle(new ExportBankFileCommand(run.Id.Value, "HDFC"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("bank_export.run_not_finalized");
    }
}
