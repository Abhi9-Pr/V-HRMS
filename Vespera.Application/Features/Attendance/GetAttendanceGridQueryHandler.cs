using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

/// <summary>
/// Two flat, projected queries (never IReadRepository+specification here — see
/// docs/CONTRIBUTING-slices.md) instead of one query per employee: the employee page first, then
/// every AttendanceDay row for exactly that page's employees and the requested range, joined in
/// memory. Two round-trips regardless of how many employees are on the page, not one per employee.
/// </summary>
public sealed class GetAttendanceGridQueryHandler : IRequestHandler<GetAttendanceGridQuery, Result<PagedResult<AttendanceGridRowDto>>>
{
    private readonly IVesperaDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetAttendanceGridQueryHandler(IVesperaDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<AttendanceGridRowDto>>> Handle(
        GetAttendanceGridQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var employeeQuery = _dbContext.Set<Employee>().Where(employee => employee.TenantId == tenantId && !employee.IsDeleted);

        if (request.DepartmentId is { } departmentId)
        {
            employeeQuery = employeeQuery.Where(employee => employee.DepartmentId == new DepartmentId(departmentId));
        }

        if (request.LocationId is { } locationId)
        {
            employeeQuery = employeeQuery.Where(employee => employee.LocationId == new LocationId(locationId));
        }

        var totalCount = await _dbContext.CountAsync(employeeQuery, cancellationToken);

        var pagedEmployeeQuery = employeeQuery
            .OrderBy(employee => employee.FirstName).ThenBy(employee => employee.LastName)
            .Skip((request.Paging.Page - 1) * request.Paging.PageSize)
            .Take(request.Paging.PageSize)
            .ProjectToType<AttendanceGridEmployeeProjection>();
        var pagedEmployees = await _dbContext.ToListAsync(pagedEmployeeQuery, cancellationToken);

        if (pagedEmployees.Count == 0)
        {
            return Result.Success(new PagedResult<AttendanceGridRowDto>([], request.Paging.Page, request.Paging.PageSize, totalCount));
        }

        var employeeIds = pagedEmployees.Select(employee => new EmployeeId(employee.EmployeeId)).ToList();

        var cellsQuery = _dbContext.Set<AttendanceDay>()
            .Where(day => day.TenantId == tenantId && employeeIds.Contains(day.EmployeeId) &&
                          day.Date >= request.RangeStart && day.Date <= request.RangeEnd)
            .ProjectToType<AttendanceGridCellProjection>();
        var cells = await _dbContext.ToListAsync(cellsQuery, cancellationToken);

        var rows = BuildRows(pagedEmployees, cells, request.RangeStart, request.RangeEnd);

        return Result.Success(new PagedResult<AttendanceGridRowDto>(rows, request.Paging.Page, request.Paging.PageSize, totalCount));
    }

    /// <summary>Pure in-memory join/reshape — no DB access, so this is the part worth unit-testing
    /// directly rather than through a mocked IQueryable (which would only prove the mock behaves
    /// as configured, not that the reshaping logic is correct). A date with no matching cell gets
    /// a null Status rather than being omitted, so every row always has exactly
    /// (rangeEnd - rangeStart + 1) days.</summary>
    public static List<AttendanceGridRowDto> BuildRows(
        IReadOnlyList<AttendanceGridEmployeeProjection> employees, IReadOnlyList<AttendanceGridCellProjection> cells,
        DateOnly rangeStart, DateOnly rangeEnd)
    {
        var cellsByEmployee = cells.ToLookup(cell => cell.EmployeeId);

        return employees.Select(employee =>
        {
            var statusByDate = cellsByEmployee[employee.EmployeeId].ToDictionary(cell => cell.Date, cell => cell.Status);
            var days = new List<AttendanceGridDayDto>();
            for (var date = rangeStart; date <= rangeEnd; date = date.AddDays(1))
            {
                days.Add(new AttendanceGridDayDto(date, statusByDate.GetValueOrDefault(date)));
            }

            return new AttendanceGridRowDto(employee.EmployeeId, employee.EmployeeCode, $"{employee.FirstName} {employee.LastName}", days);
        }).ToList();
    }
}
