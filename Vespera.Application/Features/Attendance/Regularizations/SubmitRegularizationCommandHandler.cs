using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed class SubmitRegularizationCommandHandler : IRequestHandler<SubmitRegularizationCommand, Result<Guid>>
{
    private readonly IReadRepository<AttendanceDay> _attendanceDays;
    private readonly IReadRepository<User> _users;
    private readonly IWriteRepository<RegularizationRequest> _requestWriter;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    public SubmitRegularizationCommandHandler(
        IReadRepository<AttendanceDay> attendanceDays,
        IReadRepository<User> users,
        IWriteRepository<RegularizationRequest> requestWriter,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IFileStorage fileStorage)
    {
        _attendanceDays = attendanceDays;
        _users = users;
        _requestWriter = requestWriter;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<Result<Guid>> Handle(SubmitRegularizationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var attendanceDayId = new AttendanceDayId(request.AttendanceDayId);

        var day = await _attendanceDays.FirstOrDefaultAsync(
            new AttendanceDayByIdSpecification(tenantId, attendanceDayId), cancellationToken);
        if (day is null)
        {
            return Result.Failure<Guid>(Error.NotFound("attendance_day.not_found", "Attendance day not found."));
        }

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<Guid>(Error.Unauthorized("regularization_request.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId != day.EmployeeId)
        {
            return Result.Failure<Guid>(Error.Forbidden(
                "regularization_request.self_only", "You can only submit a regularization for your own attendance day."));
        }

        string? evidenceFileReference = null;
        if (request.EvidenceContent is { Length: > 0 } content)
        {
            evidenceFileReference = await _fileStorage.UploadAsync(request.EvidenceFileName!, new MemoryStream(content), cancellationToken);
        }

        var result = RegularizationRequest.Submit(tenantId, day.EmployeeId, attendanceDayId, request.Reason, evidenceFileReference);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _requestWriter.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
