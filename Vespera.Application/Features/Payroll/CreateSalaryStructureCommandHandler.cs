using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll;

public sealed class CreateSalaryStructureCommandHandler : IRequestHandler<CreateSalaryStructureCommand, Result<Guid>>
{
    private readonly IReadRepository<SalaryStructure> _existingStructures;
    private readonly IWriteRepository<SalaryStructure> _structures;
    private readonly ITenantContext _tenantContext;

    public CreateSalaryStructureCommandHandler(
        IReadRepository<SalaryStructure> existingStructures, IWriteRepository<SalaryStructure> structures, ITenantContext tenantContext)
    {
        _existingStructures = existingStructures;
        _structures = structures;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateSalaryStructureCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var employeeId = new EmployeeId(request.EmployeeId);

        var lines = new List<SalaryStructureLine>();
        foreach (var lineRequest in request.Lines)
        {
            var formulaResult = BuildFormula(lineRequest);
            if (formulaResult.IsFailure)
            {
                return Result.Failure<Guid>(formulaResult.Error);
            }

            lines.Add(SalaryStructureLine.Of(new SalaryComponentId(lineRequest.ComponentId), formulaResult.Value));
        }

        var existing = await _existingStructures.ListAsync(
            new SalaryStructuresActiveOnDateSpecification(tenantId, request.ValidFrom), cancellationToken);
        var overlapping = existing.Where(structure => structure.EmployeeId == employeeId).ToList();

        var createResult = SalaryStructure.Create(
            tenantId, employeeId, Money.Of(request.MonthlyCtc, Currency.Inr), lines, request.ValidFrom, request.ValidTo);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var overlapResult = EffectiveDatedTimeline.EnsureNoOverlap<SalaryStructureId, SalaryStructure>(overlapping, createResult.Value);
        if (overlapResult.IsFailure)
        {
            return Result.Failure<Guid>(overlapResult.Error);
        }

        await _structures.AddAsync(createResult.Value, cancellationToken);

        return Result.Success(createResult.Value.Id.Value);
    }

    private static Result<SalaryComponentFormula> BuildFormula(SalaryStructureLineRequest request)
    {
        switch (request.FormulaKind)
        {
            case nameof(SalaryComponentFormulaKind.FixedAmount) when request.FixedAmount is { } amount:
                return Result.Success(SalaryComponentFormula.FixedAmount(Money.Of(amount, Currency.Inr)));

            case nameof(SalaryComponentFormulaKind.PercentageOfComponent) when request is { ReferenceComponentId: { } refId, Percent: { } percent }:
                return Result.Success(SalaryComponentFormula.PercentageOfComponent(new SalaryComponentId(refId), percent));

            case nameof(SalaryComponentFormulaKind.SumOfComponents) when request.SumComponentIds is { Count: > 0 } sumIds:
                return Result.Success(SalaryComponentFormula.SumOfComponents([.. sumIds.Select(id => new SalaryComponentId(id))]));

            case nameof(SalaryComponentFormulaKind.RemainderOfCtc):
                return Result.Success(SalaryComponentFormula.RemainderOfCtc());

            default:
                return Result.Failure<SalaryComponentFormula>(Error.Validation(
                    "salary_structure.invalid_line", $"Line for component {request.ComponentId} has an invalid or incomplete formula."));
        }
    }
}
