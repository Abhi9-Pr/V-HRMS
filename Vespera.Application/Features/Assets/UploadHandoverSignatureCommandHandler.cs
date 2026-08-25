using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class UploadHandoverSignatureCommandHandler : IRequestHandler<UploadHandoverSignatureCommand, Result<string>>
{
    private readonly IReadRepository<AssetAssignment> _assignments;
    private readonly IFileStorage _fileStorage;

    public UploadHandoverSignatureCommandHandler(IReadRepository<AssetAssignment> assignments, IFileStorage fileStorage)
    {
        _assignments = assignments;
        _fileStorage = fileStorage;
    }

    public async Task<Result<string>> Handle(UploadHandoverSignatureCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignments.FirstOrDefaultAsync(
            new AssetAssignmentByIdSpecification(new AssetAssignmentId(request.AssignmentId)), cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<string>(Error.NotFound("asset_assignment.not_found", "Asset assignment not found."));
        }

        var signatureReference = await _fileStorage.UploadAsync(request.FileName, new MemoryStream(request.Content), cancellationToken);

        var captureResult = assignment.CaptureHandoverSignature(signatureReference);
        if (captureResult.IsFailure)
        {
            return Result.Failure<string>(captureResult.Error);
        }

        return Result.Success(signatureReference);
    }
}
