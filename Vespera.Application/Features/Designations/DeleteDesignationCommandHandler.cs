using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

public sealed class DeleteDesignationCommandHandler : IRequestHandler<DeleteDesignationCommand, Result>
{
    private readonly IReadRepository<Designation> _designations;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteDesignationCommandHandler(
        IReadRepository<Designation> designations,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _designations = designations;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteDesignationCommand request, CancellationToken cancellationToken)
    {
        var specification = new DesignationByIdSpecification(_tenantContext.TenantId, new DesignationId(request.Id));

        var designation = await _designations.FirstOrDefaultAsync(specification, cancellationToken);
        if (designation is null)
        {
            return Result.Failure(Error.NotFound("designation.not_found", "Designation not found."));
        }

        return designation.Delete(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
