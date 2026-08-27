using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

/// <summary><see cref="Celebration.Date"/> records the original birth/joining date; the
/// recurring "next occurrence" (this year, or next year if the month/day already passed) is
/// computed here rather than stored, so there's nothing to keep in sync as years roll over.
/// Employees who opted out (<see cref="Employee.CelebrationsVisible"/> false) are excluded
/// entirely — not just hidden from their own view.</summary>
public sealed class GetUpcomingCelebrationsQueryHandler
    : IRequestHandler<GetUpcomingCelebrationsQuery, Result<IReadOnlyList<CelebrationSummaryDto>>>
{
    private readonly IReadRepository<Celebration> _celebrations;
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetUpcomingCelebrationsQueryHandler(
        IReadRepository<Celebration> celebrations, IReadRepository<Employee> employees, ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _celebrations = celebrations;
        _employees = employees;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IReadOnlyList<CelebrationSummaryDto>>> Handle(
        GetUpcomingCelebrationsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var horizon = today.AddDays(Math.Max(request.WithinDays, 0));

        var celebrations = await _celebrations.ListAsync(new CelebrationsByTenantSpecification(tenantId), cancellationToken);
        var employees = await _employees.ListAsync(new EmployeesByTenantSpecification(tenantId), cancellationToken);
        var employeesById = employees.Where(e => e.CelebrationsVisible).ToDictionary(e => e.Id);

        var upcoming = new List<CelebrationSummaryDto>();
        foreach (var celebration in celebrations)
        {
            if (!employeesById.TryGetValue(celebration.EmployeeId, out var employee))
            {
                continue;
            }

            var nextOccurrence = NextOccurrenceOnOrAfter(celebration.Date, today);
            if (nextOccurrence > horizon)
            {
                continue;
            }

            var yearsCount = nextOccurrence.Year - celebration.Date.Year;
            upcoming.Add(new CelebrationSummaryDto(
                employee.Id.Value, $"{employee.FirstName} {employee.LastName}", celebration.CelebrationType.ToString(), nextOccurrence, yearsCount));
        }

        var ordered = upcoming.OrderBy(c => c.NextOccurrence).ToList();
        return Result.Success<IReadOnlyList<CelebrationSummaryDto>>(ordered);
    }

    private static DateOnly NextOccurrenceOnOrAfter(DateOnly original, DateOnly today)
    {
        var (month, day) = (original.Month, original.Day);
        // Feb 29 in a non-leap year: observe on Feb 28, same convention as birthday-reminder
        // features generally use — there is no Feb 29 to schedule against most years.
        var daysInMonth = DateTime.DaysInMonth(today.Year, month);
        var candidate = new DateOnly(today.Year, month, Math.Min(day, daysInMonth));

        if (candidate < today)
        {
            daysInMonth = DateTime.DaysInMonth(today.Year + 1, month);
            candidate = new DateOnly(today.Year + 1, month, Math.Min(day, daysInMonth));
        }

        return candidate;
    }
}
