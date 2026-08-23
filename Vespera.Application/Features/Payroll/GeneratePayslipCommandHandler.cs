using System.Security.Cryptography;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Departments;
using Vespera.Application.Features.Designations;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll;

/// <summary>
/// Re-runs the rules pipeline for one employee on an already-Approved/Finalized/Published run to
/// recover the fine-grained <see cref="PayrollComponentLine"/> breakdown a payslip needs — the
/// dry-run step only persists the rolled-up Gross/Deductions/Net onto <see cref="PayrollRun.Lines"/>,
/// not the per-component detail. This is safe specifically because the pipeline is proven
/// deterministic (see the golden-file/reproducibility tests): re-running it on the same frozen
/// inputs reproduces the exact figures that were already approved, it does not recompute new ones.
/// </summary>
public sealed class GeneratePayslipCommandHandler : IRequestHandler<GeneratePayslipCommand, Result<Guid>>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly IReadRepository<SalaryStructure> _salaryStructures;
    private readonly IReadRepository<SalaryComponent> _salaryComponents;
    private readonly IReadRepository<StatutoryRuleSet> _statutoryRuleSets;
    private readonly IReadRepository<InvestmentDeclaration> _investmentDeclarations;
    private readonly IReadRepository<TaxRegimeVersion> _taxRegimeVersions;
    private readonly IReadRepository<LeaveRequest> _leaveRequests;
    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<Department> _departments;
    private readonly IReadRepository<Designation> _designations;
    private readonly IReadRepository<Domain.IdentityAccess.Tenant> _tenants;
    private readonly IReadRepository<Payslip> _payslips;
    private readonly IWriteRepository<Payslip> _payslipWriter;
    private readonly PayrollComputationEngine _engine;
    private readonly IPayslipRenderer _renderer;
    private readonly IPdfPasswordProtector _passwordProtector;
    private readonly IFileStorage _fileStorage;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GeneratePayslipCommandHandler(
        IReadRepository<PayrollRun> payrollRuns, IReadRepository<SalaryStructure> salaryStructures,
        IReadRepository<SalaryComponent> salaryComponents, IReadRepository<StatutoryRuleSet> statutoryRuleSets,
        IReadRepository<InvestmentDeclaration> investmentDeclarations, IReadRepository<TaxRegimeVersion> taxRegimeVersions,
        IReadRepository<LeaveRequest> leaveRequests, IReadRepository<Employee> employees, IReadRepository<Department> departments,
        IReadRepository<Designation> designations, IReadRepository<Domain.IdentityAccess.Tenant> tenants,
        IReadRepository<Payslip> payslips, IWriteRepository<Payslip> payslipWriter, PayrollComputationEngine engine,
        IPayslipRenderer renderer, IPdfPasswordProtector passwordProtector, IFileStorage fileStorage, ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _payrollRuns = payrollRuns;
        _salaryStructures = salaryStructures;
        _salaryComponents = salaryComponents;
        _statutoryRuleSets = statutoryRuleSets;
        _investmentDeclarations = investmentDeclarations;
        _taxRegimeVersions = taxRegimeVersions;
        _leaveRequests = leaveRequests;
        _employees = employees;
        _departments = departments;
        _designations = designations;
        _tenants = tenants;
        _payslips = payslips;
        _payslipWriter = payslipWriter;
        _engine = engine;
        _renderer = renderer;
        _passwordProtector = passwordProtector;
        _fileStorage = fileStorage;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(GeneratePayslipCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var payrollRunId = new PayrollRunId(request.PayrollRunId);
        var employeeId = new EmployeeId(request.EmployeeId);

        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(new PayrollRunByIdSpecification(payrollRunId), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure<Guid>(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        if (payrollRun.Status is not (PayrollRunStatus.Approved or PayrollRunStatus.Finalized or PayrollRunStatus.Published))
        {
            return Result.Failure<Guid>(Error.Conflict(
                "payslip.run_not_approved", "Payslips can only be generated once a payroll run has been approved."));
        }

        var existing = await _payslips.FirstOrDefaultAsync(
            new PayslipByRunAndEmployeeSpecification(tenantId, payrollRunId, employeeId), cancellationToken);
        if (existing is not null)
        {
            return Result.Success(existing.Id.Value);
        }

        var employee = await _employees.FirstOrDefaultAsync(new EmployeeByIdSpecification(tenantId, employeeId), cancellationToken);
        if (employee is null)
        {
            return Result.Failure<Guid>(Error.NotFound("payslip.employee_not_found", "Employee not found."));
        }

        if (employee.Pan is null)
        {
            return Result.Failure<Guid>(Error.Validation(
                "payslip.pan_required", "This employee has no PAN on record, which the payslip password scheme requires."));
        }

        var periodStart = new DateOnly(payrollRun.Year, payrollRun.Month, 1);
        var periodEnd = new DateOnly(payrollRun.Year, payrollRun.Month, DateTime.DaysInMonth(payrollRun.Year, payrollRun.Month));

        var salaryStructure = await _salaryStructures.FirstOrDefaultAsync(
            new SalaryStructureByEmployeeActiveOnDateSpecification(tenantId, employeeId, periodStart), cancellationToken);
        if (salaryStructure is null)
        {
            return Result.Failure<Guid>(Error.NotFound("payslip.no_salary_structure", "No active salary structure for this employee and period."));
        }

        var resolved = SalaryStructureResolver.ResolveMonthly(salaryStructure);
        if (resolved.IsFailure)
        {
            return Result.Failure<Guid>(resolved.Error);
        }

        var salaryComponents = await _salaryComponents.ListAsync(new AllSalaryComponentsSpecification(tenantId), cancellationToken);
        var statutoryRuleSets = await _statutoryRuleSets.ListAsync(
            new StatutoryRuleSetsActiveOnDateSpecification(tenantId, periodStart), cancellationToken);

        var lopRequests = await _leaveRequests.ListAsync(
            new ApprovedLopLeaveRequestsOverlappingPeriodSpecification(tenantId, employeeId, periodStart, periodEnd), cancellationToken);
        var lossOfPayDays = lopRequests.Sum(leaveRequest => leaveRequest.LossOfPayDays);

        var financialYear = payrollRun.Month >= 4
            ? $"{payrollRun.Year}-{(payrollRun.Year + 1) % 100:D2}"
            : $"{payrollRun.Year - 1}-{payrollRun.Year % 100:D2}";
        var declaration = await _investmentDeclarations.FirstOrDefaultAsync(
            new VerifiedInvestmentDeclarationSpecification(tenantId, employeeId, financialYear), cancellationToken);

        TaxRegimeVersion? regime = null;
        var approvedExemptions = Money.Zero(salaryStructure.MonthlyCtc.Currency);
        if (declaration is not null)
        {
            regime = await _taxRegimeVersions.FirstOrDefaultAsync(
                new TaxRegimeVersionByIdSpecification(declaration.TaxRegimeVersionId), cancellationToken);
            approvedExemptions = declaration.ApprovedExemptionTotal(salaryStructure.MonthlyCtc.Currency);
        }

        var context = new PayrollContext(
            tenantId, payrollRunId, employeeId, payrollRun.Month, payrollRun.Year, salaryStructure.MonthlyCtc.Currency,
            periodStart, periodEnd, employee.DateOfJoining, employee.ExitDate,
            resolved.Value, salaryComponents, lossOfPayDays, statutoryRuleSets, regime, approvedExemptions, [], []);

        var result = _engine.ComputeForEmployee(context);

        var department = await _departments.FirstOrDefaultAsync(new DepartmentByIdSpecification(tenantId, employee.DepartmentId), cancellationToken);
        var designation = await _designations.FirstOrDefaultAsync(
            new DesignationByIdSpecification(tenantId, employee.DesignationId), cancellationToken);
        var tenant = await _tenants.FirstOrDefaultAsync(new TenantByIdSpecification(tenantId), cancellationToken);

        var renderRequest = new PayslipRenderRequest(
            tenant?.Name ?? "Vespera", $"{employee.FirstName} {employee.LastName}", employee.Code.Value, designation?.Title ?? "-",
            department?.Name ?? "-", payrollRun.Month, payrollRun.Year, context.Currency.ToString(),
            [.. context.Lines.Where(line => line.Direction == PayrollComponentDirection.Earning)
                .Select(line => new PayslipRenderLine(line.ComponentName, line.Amount.Amount))],
            [.. context.Lines.Where(line => line.Direction == PayrollComponentDirection.Deduction)
                .Select(line => new PayslipRenderLine(line.ComponentName, line.Amount.Amount))],
            result.Gross.Amount, result.Deductions.Amount, result.Net.Amount, result.LossOfPayDays);

        var pdfBytes = _renderer.Render(renderRequest);

        // PAN+DDMM: obfuscation, not access control — see IPdfPasswordProtector's remarks and
        // /docs/security-notes.md. Real download authorization is GetPayslipDownloadUrlQuery's
        // signed URL, checked on every request.
        var password = $"{employee.Pan.Value}{employee.DateOfBirth:ddMM}";
        var protectedBytes = _passwordProtector.Protect(pdfBytes, password);
        var documentHash = Convert.ToHexString(SHA256.HashData(protectedBytes));

        var now = _dateTimeProvider.UtcNow;
        using var uploadStream = new MemoryStream(protectedBytes);
        var storageKey = await _fileStorage.UploadAsync(
            $"payslips/{payrollRun.Year:D4}-{payrollRun.Month:D2}/{employee.Code.Value}.pdf", uploadStream, cancellationToken);

        var payslip = Payslip.Generate(tenantId, payrollRunId, employeeId, result.Net, context.Lines, now);
        var attachResult = payslip.AttachDocument(storageKey, documentHash, now);
        if (attachResult.IsFailure)
        {
            return Result.Failure<Guid>(attachResult.Error);
        }

        await _payslipWriter.AddAsync(payslip, cancellationToken);

        return Result.Success(payslip.Id.Value);
    }
}
