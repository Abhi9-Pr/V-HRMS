using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Self-service delta sync for the caller's own leave requests — resolves "which
/// employee is this caller" via the shared <see cref="CurrentEmployeeResolver"/>. A caller with
/// no linked employee (e.g. a pure admin account) gets a well-formed, empty result rather than a
/// rejection.</summary>
public sealed class GetMyLeaveDeltaSyncQueryHandler
    : DeltaSyncQueryHandlerBase<GetMyLeaveDeltaSyncQuery, LeaveRequest, LeaveRequestSyncDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetMyLeaveDeltaSyncQueryHandler(
        IReadRepository<LeaveRequest> repository,
        IDateTimeProvider dateTimeProvider,
        ITenantContext tenantContext,
        CurrentEmployeeResolver currentEmployeeResolver)
        : base(repository, dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    protected override DeltaSyncRequest GetDeltaSyncRequest(GetMyLeaveDeltaSyncQuery request) =>
        new(request.Since, request.Cursor, request.PageSize <= 0 ? 100 : request.PageSize);

    protected override async Task<ISpecification<LeaveRequest>> BuildSpecification(
        GetMyLeaveDeltaSyncQuery request, DeltaSyncRequest deltaSync, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken) ?? EmployeeId.New();
        return new LeaveRequestsByEmployeeSpecification(_tenantContext.TenantId, employeeId);
    }

    protected override Guid GetId(LeaveRequest entity) => entity.Id.Value;

    protected override DateTimeOffset GetLastChanged(LeaveRequest entity) => entity.ModifiedAt ?? entity.CreatedAt;

    protected override bool IsTombstoned(LeaveRequest entity) => false;

    protected override LeaveRequestSyncDto MapToDto(LeaveRequest entity) => new(
        entity.Id.Value, entity.EmployeeId.Value, entity.LeaveTypeId.Value,
        entity.Period.Start, entity.Period.End, entity.RequestedDays, entity.Status.ToString());
}
