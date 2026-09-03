using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

/// <summary>The prerequisite <see cref="SalaryStructuresController"/>'s Create action was always
/// missing an HTTP-reachable way to satisfy: a <c>CreateSalaryStructureCommand</c> line references
/// a component by id, but until this slice there was no endpoint that could mint one — only
/// direct in-process construction (<c>SalaryComponent.Create</c>) in tests/seed data. Found while
/// building the payroll-dry-run load test, which needs real salary structures at scale.</summary>
public sealed record CreateSalaryComponentCommand(string Name, string ComponentType, bool IsTaxable) : IRequest<Result<Guid>>;
