using Microsoft.AspNetCore.Authorization;

namespace Vespera.Api.Authorization;

/// <summary>The second maker-checker check for payroll finalize: "the user who ran the dry run
/// cannot finalize" is not the same person as "the creator" — one person can open a run, a
/// different person can trigger the compute. Payroll-specific (unlike <see cref="NotCreatorRequirement"/>)
/// since <c>DryRunExecutedBy</c> only exists on <c>PayrollRun</c>.</summary>
public sealed class NotDryRunExecutorRequirement : IAuthorizationRequirement
{
}
