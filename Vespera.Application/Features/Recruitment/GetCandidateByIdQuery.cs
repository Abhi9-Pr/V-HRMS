using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed record GetCandidateByIdQuery(Guid Id) : IRequest<Result<CandidateDto>>;

public sealed record CandidateDto(
    Guid Id, Guid JobRequisitionId, string FullName, string Email, string Phone, CandidateStatus Status, Guid? CurrentPipelineStageId);
