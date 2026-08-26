using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record SalaryStructureLineDto(Guid ComponentId, string FormulaKind, decimal? FixedAmount, Guid? ReferenceComponentId, decimal? Percent);

public sealed record SalaryStructureDto(
    Guid Id, Guid EmployeeId, decimal MonthlyCtc, DateOnly ValidFrom, DateOnly? ValidTo, IReadOnlyList<SalaryStructureLineDto> Lines);

public sealed record GetSalaryStructureQuery(Guid EmployeeId, DateOnly AsOf) : IRequest<Result<SalaryStructureDto?>>;
