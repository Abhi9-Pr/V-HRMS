using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

public sealed record SubmitOnboardingCommand(Guid OnboardingDraftId, string EmployeeCode) : IRequest<Result<Guid>>;
