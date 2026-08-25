using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class RecordAssetConditionCommandHandler : IRequestHandler<RecordAssetConditionCommand, Result>
{
    private readonly IReadRepository<AssetAssignment> _assignments;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordAssetConditionCommandHandler(
        IReadRepository<AssetAssignment> assignments, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _assignments = assignments;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RecordAssetConditionCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignments.FirstOrDefaultAsync(
            new AssetAssignmentByIdSpecification(new AssetAssignmentId(request.AssignmentId)), cancellationToken);

        if (assignment is null)
        {
            return Result.Failure(Error.NotFound("asset_assignment.not_found", "Asset assignment not found."));
        }

        return assignment.RecordConditionReport(
            request.Rating, request.Notes, _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
