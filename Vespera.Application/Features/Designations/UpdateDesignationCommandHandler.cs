using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

public sealed class UpdateDesignationCommandHandler : IRequestHandler<UpdateDesignationCommand, Result>
{
    private readonly IReadRepository<Designation> _designations;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateDesignationCommandHandler(
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

    public async Task<Result> Handle(UpdateDesignationCommand request, CancellationToken cancellationToken)
    {
        var specification = new DesignationByIdSpecification(_tenantContext.TenantId, new DesignationId(request.Id));

        var designation = await _designations.FirstOrDefaultAsync(specification, cancellationToken);
        if (designation is null)
        {
            return Result.Failure(Error.NotFound("designation.not_found", "Designation not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        var renameResult = designation.Rename(request.Title, now, modifiedBy);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        return designation.ChangeGrade(request.Grade, now, modifiedBy);
    }
}
