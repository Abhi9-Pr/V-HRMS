using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Application.Authorization;

namespace Vespera.Api.Controllers.V1.Finance;

/// <summary>
/// The finance policy wall: every controller under <c>/api/v1/finance</c> derives from this base
/// and inherits the <c>Finance.Admin</c> requirement without repeating the attribute. HR and
/// SysAdmin roles alone are insufficient — Finance.Admin is a separately granted permission, not
/// implied by any role name (see DevelopmentSeeder and the Phase 4 plan, decision 5).
/// </summary>
[HasPermission(Permissions.Finance.Admin)]
public abstract class FinanceControllerBase : ControllerBase
{
}
