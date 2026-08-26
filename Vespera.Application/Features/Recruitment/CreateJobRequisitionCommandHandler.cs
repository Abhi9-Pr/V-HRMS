using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class CreateJobRequisitionCommandHandler : IRequestHandler<CreateJobRequisitionCommand, Result<Guid>>
{
    private readonly IWriteRepository<JobRequisition> _requisitions;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateJobRequisitionCommandHandler(
        IWriteRepository<JobRequisition> requisitions, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _requisitions = requisitions;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateJobRequisitionCommand request, CancellationToken cancellationToken)
    {
        var result = JobRequisition.Create(
            _tenantContext.TenantId, request.Title, new DepartmentId(request.DepartmentId), request.OpeningsCount,
            _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _requisitions.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
