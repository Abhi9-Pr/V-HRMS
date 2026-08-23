using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

public sealed record UpdateOnboardingEmploymentDetailsCommand(
    Guid OnboardingDraftId,
    Guid DepartmentId,
    Guid DesignationId,
    Guid LocationId,
    DateOnly DateOfJoining) : IRequest<Result>;
