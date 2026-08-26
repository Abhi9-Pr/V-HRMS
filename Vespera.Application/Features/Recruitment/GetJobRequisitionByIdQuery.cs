using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record GetJobRequisitionByIdQuery(Guid Id) : IRequest<Result<JobRequisitionDto>>;
