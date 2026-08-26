using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record ReviewInvestmentDeclarationLineCommand(Guid DeclarationId, int LineIndex, bool Approved, string? Comment) : IRequest<Result>;
