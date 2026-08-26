using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

/// <summary>Creates the caller's Draft declaration for <paramref name="FinancialYear"/> on first
/// call (get-or-create) and adds a line to it — the employee never has to explicitly "start" a
/// declaration before adding their first investment.</summary>
public sealed record AddInvestmentDeclarationLineCommand(
    string FinancialYear, Guid TaxRegimeVersionId, string Section, decimal Amount, string? ProofFileReference)
    : IRequest<Result<Guid>>;
