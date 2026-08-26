using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class GetSalaryStructureQueryHandler : IRequestHandler<GetSalaryStructureQuery, Result<SalaryStructureDto?>>
{
    private readonly IReadRepository<SalaryStructure> _structures;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public GetSalaryStructureQueryHandler(
        IReadRepository<SalaryStructure> structures, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor)
    {
        _structures = structures;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<SalaryStructureDto?>> Handle(GetSalaryStructureQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var employeeId = new EmployeeId(request.EmployeeId);

        var structure = await _structures.FirstOrDefaultAsync(
            new SalaryStructureByEmployeeActiveOnDateSpecification(tenantId, employeeId, request.AsOf), cancellationToken);
        if (structure is null)
        {
            return Result.Success<SalaryStructureDto?>(null);
        }

        await _piiAccessAuditor.RecordAccessAsync(
            tenantId, "SalaryStructure", structure.Id.Value, "Lines", _currentUser.UserId?.ToString() ?? "system",
            _dateTimeProvider.UtcNow, cancellationToken);

        var lines = structure.Lines
            .Select(line => new SalaryStructureLineDto(
                line.ComponentId.Value, line.Formula.Kind.ToString(), line.Formula.FixedAmountValue?.Amount,
                line.Formula.ReferenceComponentId?.Value, line.Formula.Percent))
            .ToList();

        return Result.Success<SalaryStructureDto?>(new SalaryStructureDto(
            structure.Id.Value, structure.EmployeeId.Value, structure.MonthlyCtc.Amount, structure.ValidFrom, structure.ValidTo, lines));
    }
}
