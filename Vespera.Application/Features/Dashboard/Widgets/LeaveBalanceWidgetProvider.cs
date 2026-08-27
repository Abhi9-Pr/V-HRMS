using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed record LeaveBalanceWidgetRowDto(Guid LeaveTypeId, string LeaveTypeName, decimal Available);

public sealed record LeaveBalanceWidgetDto(IReadOnlyList<LeaveBalanceWidgetRowDto> Balances);

public sealed class LeaveBalanceWidgetProvider : IDashboardWidgetProvider
{
    private readonly IReadRepository<LeaveBalance> _balances;
    private readonly IReadRepository<LeaveType> _leaveTypes;
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public LeaveBalanceWidgetProvider(
        IReadRepository<LeaveBalance> balances, IReadRepository<LeaveType> leaveTypes, ITenantContext tenantContext,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _balances = balances;
        _leaveTypes = leaveTypes;
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public string WidgetKey => "leaveBalance";

    public int DefaultOrder => 6;

    public bool DefaultVisible => true;

    public WidgetSize DefaultSize => WidgetSize.Small;

    public async Task<Result<object?>> GetPayloadAsync(CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Success<object?>(new LeaveBalanceWidgetDto([]));
        }

        var tenantId = _tenantContext.TenantId;
        var balances = await _balances.ListAsync(new LeaveBalancesByEmployeeSpecification(tenantId, employeeId.Value), cancellationToken);
        var leaveTypes = await _leaveTypes.ListAsync(new LeaveTypesByTenantSpecification(tenantId), cancellationToken);
        var namesById = leaveTypes.ToDictionary(t => t.Id, t => t.Name);

        var rows = balances
            .Where(b => namesById.ContainsKey(b.LeaveTypeId))
            .Select(b => new LeaveBalanceWidgetRowDto(b.LeaveTypeId.Value, namesById[b.LeaveTypeId], b.Available))
            .OrderBy(row => row.LeaveTypeName)
            .ToList();

        return Result.Success<object?>(new LeaveBalanceWidgetDto(rows));
    }
}
