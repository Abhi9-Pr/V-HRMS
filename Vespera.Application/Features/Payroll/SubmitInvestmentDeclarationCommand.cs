using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record SubmitInvestmentDeclarationCommand(string FinancialYear) : IRequest<Result>;
