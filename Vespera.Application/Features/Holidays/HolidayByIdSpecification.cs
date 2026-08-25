using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

public sealed class HolidayByIdSpecification : ISpecification<Holiday>
{
    public HolidayByIdSpecification(TenantId tenantId, HolidayId holidayId)
    {
        Criteria = holiday => holiday.TenantId == tenantId && holiday.Id == holidayId && !holiday.IsDeleted;
    }

    public Expression<Func<Holiday, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Holiday, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Holiday, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
