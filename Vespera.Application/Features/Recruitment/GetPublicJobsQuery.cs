using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record GetPublicJobsQuery : IRequest<Result<IReadOnlyList<PublicJobDto>>>;

public sealed record PublicJobDto(Guid Id, string Title, string DepartmentName, int OpeningsCount);
