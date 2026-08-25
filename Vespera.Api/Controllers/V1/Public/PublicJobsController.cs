using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Vespera.Api.Extensions;
using Vespera.Api.Http;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Api.Controllers.V1.Public;

/// <summary>The anonymous careers page's read-only job feed. No auth, aggressively cached, and
/// rate-limited on its own (looser) policy than the rest of the API — see
/// ObservabilityServiceCollectionExtensions. Tenant identity still comes from the ambient
/// ITenantContext via the pre-auth X-Tenant-Id header (see GetPublicJobsQueryHandler): a real
/// careers page always knows its own employer's tenant id.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/public/jobs")]
[EnableRateLimiting(ObservabilityServiceCollectionExtensions.PublicApiRateLimitPolicyName)]
public sealed class PublicJobsController : ControllerBase
{
    private readonly ISender _sender;

    public PublicJobsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = ObservabilityServiceCollectionExtensions.PublicJobsOutputCachePolicyName)]
    public async Task<IActionResult> GetPublicJobs(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetPublicJobsQuery(), cancellationToken)).ToActionResult(this);
}
