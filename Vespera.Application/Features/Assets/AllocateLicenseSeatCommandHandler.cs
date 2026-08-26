using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Assets;

public sealed class AllocateLicenseSeatCommandHandler : IRequestHandler<AllocateLicenseSeatCommand, Result<Guid>>
{
    private readonly IReadRepository<SoftwareLicense> _licenses;
    private readonly IWriteRepository<SoftwareLicenseAllocation> _allocations;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AllocateLicenseSeatCommandHandler(
        IReadRepository<SoftwareLicense> licenses, IWriteRepository<SoftwareLicenseAllocation> allocations,
        ITenantContext tenantContext, IDateTimeProvider dateTimeProvider)
    {
        _licenses = licenses;
        _allocations = allocations;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AllocateLicenseSeatCommand request, CancellationToken cancellationToken)
    {
        var license = await _licenses.FirstOrDefaultAsync(
            new SoftwareLicenseByIdSpecification(new SoftwareLicenseId(request.LicenseId)), cancellationToken);

        if (license is null)
        {
            return Result.Failure<Guid>(Error.NotFound("software_license.not_found", "Software license not found."));
        }

        var assignSeatResult = license.AssignSeat();
        if (assignSeatResult.IsFailure)
        {
            return Result.Failure<Guid>(assignSeatResult.Error);
        }

        var allocation = SoftwareLicenseAllocation.Allocate(
            _tenantContext.TenantId, license.Id, new EmployeeId(request.EmployeeId), _dateTimeProvider.UtcNow);
        await _allocations.AddAsync(allocation, cancellationToken);

        return Result.Success(allocation.Id.Value);
    }
}
