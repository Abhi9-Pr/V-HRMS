using MediatR;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record ExportBankFileCommand(Guid PayrollRunId, string BankCode) : IRequest<Result<BankFileExportResult>>;
