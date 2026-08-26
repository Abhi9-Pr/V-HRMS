using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record SubmitPayrollRunForReviewCommand(Guid PayrollRunId) : IRequest<Result>;
