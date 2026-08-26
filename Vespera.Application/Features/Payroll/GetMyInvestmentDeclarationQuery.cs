using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record InvestmentDeclarationLineDto(
    int LineIndex, string Section, decimal Amount, string? ProofFileReference, string ReviewStatus, string? ReviewComment);

public sealed record InvestmentDeclarationDto(
    Guid Id, string FinancialYear, string Status, IReadOnlyList<InvestmentDeclarationLineDto> Lines);

public sealed record GetMyInvestmentDeclarationQuery(string FinancialYear) : IRequest<Result<InvestmentDeclarationDto?>>;
