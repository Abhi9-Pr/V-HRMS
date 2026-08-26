using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class AddPipelineStageCommandHandler : IRequestHandler<AddPipelineStageCommand, Result>
{
    private readonly IWriteRepository<JobRequisition> _requisitions;
    private readonly IReadRepository<JobRequisition> _requisitionReads;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddPipelineStageCommandHandler(
        IWriteRepository<JobRequisition> requisitions, IReadRepository<JobRequisition> requisitionReads, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _requisitions = requisitions;
        _requisitionReads = requisitionReads;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AddPipelineStageCommand request, CancellationToken cancellationToken)
    {
        var requisition = await _requisitionReads.FirstOrDefaultAsync(
            new JobRequisitionByIdSpecification(new JobRequisitionId(request.RequisitionId)), cancellationToken);

        if (requisition is null)
        {
            return Result.Failure(Error.NotFound("job_requisition.not_found", "Job requisition not found."));
        }

        var result = requisition.AddStage(request.StageName, _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
        if (result.IsFailure)
        {
            return result;
        }

        _requisitions.Update(requisition);
        return Result.Success();
    }
}
