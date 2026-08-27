using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record GetUpcomingCelebrationsQuery(int WithinDays = 30) : IRequest<Result<IReadOnlyList<CelebrationSummaryDto>>>;

public sealed record CelebrationSummaryDto(Guid EmployeeId, string EmployeeName, string CelebrationType, DateOnly NextOccurrence, int YearsCount);
