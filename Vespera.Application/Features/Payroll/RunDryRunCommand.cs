using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

/// <summary>The maker-checker "maker" step: whoever sends this becomes <c>PayrollRun.DryRunExecutedBy</c>,
/// and is later blocked from finalizing this same run (see <c>NotDryRunExecutorRequirement</c>).</summary>
public sealed record RunDryRunCommand(Guid PayrollRunId) : IRequest<Result>;
