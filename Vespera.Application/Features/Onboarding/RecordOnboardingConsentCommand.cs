using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

public sealed record RecordOnboardingConsentCommand(Guid OnboardingDraftId, string ConsentType) : IRequest<Result>;
