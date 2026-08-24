using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

/// <summary>Every holiday in the tenant, unpaged — the calendar is small enough that a full read
/// is fine, and <see cref="Domain.Services.BusinessHoursCalculator"/> needs the whole set anyway.</summary>
public sealed class PublicHolidaysByTenantSpecification : ISpecification<PublicHoliday>
{
    public PublicHolidaysByTenantSpecification(TenantId tenantId)
    {
        Criteria = holiday => holiday.TenantId == tenantId;
    }

    public Expression<Func<PublicHoliday, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<PublicHoliday, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<PublicHoliday, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
