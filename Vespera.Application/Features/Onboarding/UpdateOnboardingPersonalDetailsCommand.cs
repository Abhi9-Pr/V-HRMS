using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

public sealed record UpdateOnboardingPersonalDetailsCommand(
    Guid OnboardingDraftId,
    string FirstName,
    string LastName,
    string WorkEmail,
    string Phone,
    DateOnly DateOfBirth) : IRequest<Result>;
