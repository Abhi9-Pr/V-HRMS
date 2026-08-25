using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Onboarding;

public sealed record StartOnboardingDraftCommand : IRequest<Result<Guid>>;
