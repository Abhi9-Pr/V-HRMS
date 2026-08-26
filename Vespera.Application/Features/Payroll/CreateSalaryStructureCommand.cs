using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

/// <summary>One formula line's request shape — exactly one of <see cref="FixedAmount"/>,
/// (<see cref="ReferenceComponentId"/>, <see cref="Percent"/>), or <see cref="SumComponentIds"/>
/// must be set depending on <see cref="FormulaKind"/>; a <c>RemainderOfCtc</c> line needs none of
/// them.</summary>
public sealed record SalaryStructureLineRequest(
    Guid ComponentId, string FormulaKind, decimal? FixedAmount, Guid? ReferenceComponentId, decimal? Percent,
    IReadOnlyList<Guid>? SumComponentIds);

public sealed record CreateSalaryStructureCommand(
    Guid EmployeeId, decimal MonthlyCtc, IReadOnlyList<SalaryStructureLineRequest> Lines, DateOnly ValidFrom, DateOnly? ValidTo)
    : IRequest<Result<Guid>>;
