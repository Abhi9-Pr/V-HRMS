using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class CreateSoftwareLicenseCommandHandler : IRequestHandler<CreateSoftwareLicenseCommand, Result<Guid>>
{
    private readonly IWriteRepository<SoftwareLicense> _licenses;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSoftwareLicenseCommandHandler(
        IWriteRepository<SoftwareLicense> licenses, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _licenses = licenses;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateSoftwareLicenseCommand request, CancellationToken cancellationToken)
    {
        var result = SoftwareLicense.Create(
            _tenantContext.TenantId, request.ProductName, request.SeatCount, request.ExpiresAt,
            _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _licenses.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
