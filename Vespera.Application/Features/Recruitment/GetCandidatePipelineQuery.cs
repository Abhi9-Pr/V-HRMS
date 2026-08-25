using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed record GetCandidatePipelineQuery(Guid JobRequisitionId) : IRequest<Result<CandidatePipelineDto>>;

public sealed record CandidateCardDto(Guid Id, string FullName, CandidateStatus Status, Guid? CurrentPipelineStageId);

public sealed record CandidatePipelineDto(IReadOnlyList<PipelineStageDto> Stages, IReadOnlyList<CandidateCardDto> Candidates);
