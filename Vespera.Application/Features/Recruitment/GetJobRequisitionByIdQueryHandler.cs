using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetJobRequisitionByIdQueryHandler : IRequestHandler<GetJobRequisitionByIdQuery, Result<JobRequisitionDto>>
{
    private readonly IReadRepository<JobRequisition> _requisitions;

    public GetJobRequisitionByIdQueryHandler(IReadRepository<JobRequisition> requisitions)
    {
        _requisitions = requisitions;
    }

    public async Task<Result<JobRequisitionDto>> Handle(GetJobRequisitionByIdQuery request, CancellationToken cancellationToken)
    {
        var requisition = await _requisitions.FirstOrDefaultAsync(
            new JobRequisitionByIdSpecification(new JobRequisitionId(request.Id)), cancellationToken);

        return requisition is null
            ? Result.Failure<JobRequisitionDto>(Error.NotFound("job_requisition.not_found", "Job requisition not found."))
            : Result.Success(requisition.Adapt<JobRequisitionDto>());
    }
}
