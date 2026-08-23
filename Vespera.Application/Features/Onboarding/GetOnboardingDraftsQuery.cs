using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

public sealed record GetOnboardingDraftsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<OnboardingDraftSummaryDto>>>;

public sealed record OnboardingDraftSummaryDto(Guid Id, string Status, string CurrentStep, string? FirstName, string? LastName, DateTimeOffset CreatedAt);
