using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class PublishRequisitionCommandHandler : IRequestHandler<PublishRequisitionCommand, Result>
{
    private readonly IReadRepository<JobRequisition> _requisitionReads;
    private readonly IWriteRepository<JobRequisition> _requisitions;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PublishRequisitionCommandHandler(
        IReadRepository<JobRequisition> requisitionReads, IWriteRepository<JobRequisition> requisitions, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _requisitionReads = requisitionReads;
        _requisitions = requisitions;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(PublishRequisitionCommand request, CancellationToken cancellationToken)
    {
        var requisition = await _requisitionReads.FirstOrDefaultAsync(
            new JobRequisitionByIdSpecification(new JobRequisitionId(request.RequisitionId)), cancellationToken);

        if (requisition is null)
        {
            return Result.Failure(Error.NotFound("job_requisition.not_found", "Job requisition not found."));
        }

        var result = requisition.Publish(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
        if (result.IsFailure)
        {
            return result;
        }

        _requisitions.Update(requisition);
        return Result.Success();
    }
}
