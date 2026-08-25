using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class ReleaseLicenseSeatCommandHandler : IRequestHandler<ReleaseLicenseSeatCommand, Result>
{
    private readonly IReadRepository<SoftwareLicenseAllocation> _allocations;
    private readonly IReadRepository<SoftwareLicense> _licenses;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReleaseLicenseSeatCommandHandler(
        IReadRepository<SoftwareLicenseAllocation> allocations, IReadRepository<SoftwareLicense> licenses, IDateTimeProvider dateTimeProvider)
    {
        _allocations = allocations;
        _licenses = licenses;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ReleaseLicenseSeatCommand request, CancellationToken cancellationToken)
    {
        var allocation = await _allocations.FirstOrDefaultAsync(
            new SoftwareLicenseAllocationByIdSpecification(new SoftwareLicenseAllocationId(request.AllocationId)), cancellationToken);

        if (allocation is null)
        {
            return Result.Failure(Error.NotFound("software_license_allocation.not_found", "Software license allocation not found."));
        }

        var releaseResult = allocation.Release(_dateTimeProvider.UtcNow);
        if (releaseResult.IsFailure)
        {
            return releaseResult;
        }

        var license = await _licenses.FirstOrDefaultAsync(new SoftwareLicenseByIdSpecification(allocation.LicenseId), cancellationToken);
        if (license is null)
        {
            return Result.Failure(Error.NotFound("software_license.not_found", "Software license not found."));
        }

        return license.ReleaseSeat();
    }
}
