using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed record GetJobRequisitionsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<JobRequisitionDto>>>;

public sealed record PipelineStageDto(Guid Id, string Name, int SequenceNumber);

public sealed record JobRequisitionDto(
    Guid Id, string Title, Guid DepartmentId, int OpeningsCount, JobRequisitionStatus Status,
    RequisitionApprovalStatus ApprovalStatus, bool IsPublished, string? RejectionReason, IReadOnlyList<PipelineStageDto> Stages);
