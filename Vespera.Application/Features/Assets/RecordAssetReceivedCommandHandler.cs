using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class RecordAssetReceivedCommandHandler : IRequestHandler<RecordAssetReceivedCommand, Result>
{
    private readonly IReadRepository<AssetRecovery> _recoveries;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordAssetReceivedCommandHandler(IReadRepository<AssetRecovery> recoveries, IDateTimeProvider dateTimeProvider)
    {
        _recoveries = recoveries;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RecordAssetReceivedCommand request, CancellationToken cancellationToken)
    {
        var recovery = await _recoveries.FirstOrDefaultAsync(
            new AssetRecoveryByIdSpecification(new AssetRecoveryId(request.RecoveryId)), cancellationToken);

        if (recovery is null)
        {
            return Result.Failure(Error.NotFound("asset_recovery.not_found", "Asset recovery not found."));
        }

        return recovery.RecordReceived(_dateTimeProvider.UtcNow);
    }
}
