using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class UpdateLeaveTypeEligibilityCommandHandler : IRequestHandler<UpdateLeaveTypeEligibilityCommand, Result>
{
    private readonly IReadRepository<LeaveType> _leaveTypes;
    private readonly IWriteRepository<LeaveType> _writer;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateLeaveTypeEligibilityCommandHandler(
        IReadRepository<LeaveType> leaveTypes, IWriteRepository<LeaveType> writer, ITenantContext tenantContext,
        ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _leaveTypes = leaveTypes;
        _writer = writer;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateLeaveTypeEligibilityCommand request, CancellationToken cancellationToken)
    {
        var leaveType = await _leaveTypes.FirstOrDefaultAsync(
            new LeaveTypeByIdSpecification(_tenantContext.TenantId, new LeaveTypeId(request.LeaveTypeId)), cancellationToken);
        if (leaveType is null)
        {
            return Result.Failure(Error.NotFound("leave_type.not_found", "Leave type not found."));
        }

        var gender = request.ApplicableGender is { } name ? Enum.Parse<Gender>(name) : (Gender?)null;
        var result = leaveType.UpdateEligibilityRules(
            gender, request.MinimumTenureMonths, request.IsEncashable, request.MaxEncashableDays, _dateTimeProvider.UtcNow,
            _currentUser.Email ?? "system");
        if (result.IsFailure)
        {
            return result;
        }

        _writer.Update(leaveType);
        return Result.Success();
    }
}
