using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Departments;
using Vespera.Application.Features.Designations;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Payroll;
using Vespera.Application.Features.Payroll.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GeneratePayslipCommandHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly IReadRepository<SalaryStructure> _salaryStructures = Substitute.For<IReadRepository<SalaryStructure>>();
    private readonly IReadRepository<SalaryComponent> _salaryComponents = Substitute.For<IReadRepository<SalaryComponent>>();
    private readonly IReadRepository<StatutoryRuleSet> _statutoryRuleSets = Substitute.For<IReadRepository<StatutoryRuleSet>>();
    private readonly IReadRepository<InvestmentDeclaration> _investmentDeclarations = Substitute.For<IReadRepository<InvestmentDeclaration>>();
    private readonly IReadRepository<TaxRegimeVersion> _taxRegimeVersions = Substitute.For<IReadRepository<TaxRegimeVersion>>();
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly IReadRepository<Designation> _designations = Substitute.For<IReadRepository<Designation>>();
    private readonly IReadRepository<Domain.IdentityAccess.Tenant> _tenants = Substitute.For<IReadRepository<Domain.IdentityAccess.Tenant>>();
    private readonly IReadRepository<Payslip> _payslips = Substitute.For<IReadRepository<Payslip>>();
    private readonly IWriteRepository<Payslip> _payslipWriter = Substitute.For<IWriteRepository<Payslip>>();
    private readonly IPayslipRenderer _renderer = Substitute.For<IPayslipRenderer>();
    private readonly IPdfPasswordProtector _passwordProtector = Substitute.For<IPdfPasswordProtector>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private GeneratePayslipCommandHandler CreateHandler() => new(
        _payrollRuns, _salaryStructures, _salaryComponents, _statutoryRuleSets, _investmentDeclarations, _taxRegimeVersions,
        _leaveRequests, _employees, _departments, _designations, _tenants, _payslips, _payslipWriter,
        new PayrollComputationEngine([new EarningsRule(), new NetPayRule()]), _renderer, _passwordProtector, _fileStorage,
        _tenantContext, _dateTimeProvider);

    private static Employee OnboardWithPan(TenantId tenantId)
    {
        var employee = Employee.Onboard(
            tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
            EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            DateTimeOffset.UtcNow, "seed").Value;
        employee.UpdateStatutoryDetails(PanNumber.Create("ABCDE1234F").Value, null, DateTimeOffset.UtcNow, "seed");
        return employee;
    }

    private void SetUpCommonDependencies(EmployeeId employeeId)
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var component = SalaryComponent.Create(_tenantId, "Basic", SalaryComponentType.Earning, isTaxable: true, DateTimeOffset.UtcNow, "seed").Value;
        _salaryComponents.ListAsync(Arg.Any<AllSalaryComponentsSpecification>(), Arg.Any<CancellationToken>()).Returns([component]);
        _statutoryRuleSets.ListAsync(Arg.Any<StatutoryRuleSetsActiveOnDateSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var structure = SalaryStructure.Create(
            _tenantId, employeeId, Money.Of(40000m, Currency.Inr),
            [SalaryStructureLine.Of(component.Id, SalaryComponentFormula.FixedAmount(Money.Of(40000m, Currency.Inr)))],
            new DateOnly(2026, 1, 1), null).Value;
        _salaryStructures.FirstOrDefaultAsync(Arg.Any<SalaryStructureByEmployeeActiveOnDateSpecification>(), Arg.Any<CancellationToken>())
            .Returns(structure);

        _leaveRequests.ListAsync(Arg.Any<ApprovedLopLeaveRequestsOverlappingPeriodSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
        _investmentDeclarations.FirstOrDefaultAsync(Arg.Any<VerifiedInvestmentDeclarationSpecification>(), Arg.Any<CancellationToken>())
            .Returns((InvestmentDeclaration?)null);
        _departments.FirstOrDefaultAsync(Arg.Any<DepartmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Department?)null);
        _designations.FirstOrDefaultAsync(Arg.Any<DesignationByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Designation?)null);
        _tenants.FirstOrDefaultAsync(Arg.Any<TenantByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Domain.IdentityAccess.Tenant?)null);

        _renderer.Render(Arg.Any<PayslipRenderRequest>()).Returns([1, 2, 3]);
        _passwordProtector.Protect(Arg.Any<byte[]>(), Arg.Any<string>()).Returns([4, 5, 6]);
        _fileStorage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns("payslips/key.pdf");

        _payslips.FirstOrDefaultAsync(Arg.Any<PayslipByRunAndEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((Payslip?)null);
    }

    [Fact]
    public async Task Handle_Should_Generate_And_Persist_A_Payslip_For_An_Approved_Run()
    {
        var now = DateTimeOffset.UtcNow;
        var payrollRun = PayrollRun.Open(_tenantId, 5, 2026, now, "seed").Value;
        payrollRun.FreezeAttendance(new DateOnly(2026, 5, 26), 25, now, "seed");
        var employee = OnboardWithPan(_tenantId);
        payrollRun.RecomputeLines(
            [new PayrollLineInput(employee.Id, Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0)],
            "seed", now);
        payrollRun.SubmitForReview();
        payrollRun.Approve("seed", now);

        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payrollRun);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);
        SetUpCommonDependencies(employee.Id);

        var result = await CreateHandler().Handle(new GeneratePayslipCommand(payrollRun.Id.Value, employee.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _payslipWriter.Received(1).AddAsync(Arg.Any<Payslip>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_The_Existing_Payslip_Id_Without_Regenerating_When_One_Already_Exists()
    {
        var now = DateTimeOffset.UtcNow;
        var payrollRun = PayrollRun.Open(_tenantId, 5, 2026, now, "seed").Value;
        payrollRun.FreezeAttendance(new DateOnly(2026, 5, 26), 25, now, "seed");
        var employee = OnboardWithPan(_tenantId);
        payrollRun.RecomputeLines(
            [new PayrollLineInput(employee.Id, Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0)],
            "seed", now);
        payrollRun.SubmitForReview();
        payrollRun.Approve("seed", now);

        _tenantContext.TenantId.Returns(_tenantId);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payrollRun);

        var existing = Payslip.Generate(_tenantId, payrollRun.Id, employee.Id, Money.Of(40000m, Currency.Inr), [], now);
        _payslips.FirstOrDefaultAsync(Arg.Any<PayslipByRunAndEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await CreateHandler().Handle(new GeneratePayslipCommand(payrollRun.Id.Value, employee.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id.Value);
        await _payslipWriter.DidNotReceive().AddAsync(Arg.Any<Payslip>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Does_Not_Exist()
    {
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        var result = await CreateHandler().Handle(new GeneratePayslipCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Has_Not_Been_Approved()
    {
        var now = DateTimeOffset.UtcNow;
        var payrollRun = PayrollRun.Open(_tenantId, 5, 2026, now, "seed").Value;
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payrollRun);

        var result = await CreateHandler().Handle(
            new GeneratePayslipCommand(payrollRun.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payslip.run_not_approved");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Employee_Has_No_Pan_On_Record()
    {
        var now = DateTimeOffset.UtcNow;
        var payrollRun = PayrollRun.Open(_tenantId, 5, 2026, now, "seed").Value;
        payrollRun.FreezeAttendance(new DateOnly(2026, 5, 26), 25, now, "seed");
        var employee = Employee.Onboard(
            _tenantId, EmployeeCode.Create("EMP-101").Value, "No", "Pan",
            EmailAddress.Create("nopan@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            DateTimeOffset.UtcNow, "seed").Value;
        payrollRun.RecomputeLines(
            [new PayrollLineInput(employee.Id, Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0)],
            "seed", now);
        payrollRun.SubmitForReview();
        payrollRun.Approve("seed", now);

        _tenantContext.TenantId.Returns(_tenantId);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payrollRun);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);
        _payslips.FirstOrDefaultAsync(Arg.Any<PayslipByRunAndEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((Payslip?)null);

        var result = await CreateHandler().Handle(
            new GeneratePayslipCommand(payrollRun.Id.Value, employee.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payslip.pan_required");
    }
}
