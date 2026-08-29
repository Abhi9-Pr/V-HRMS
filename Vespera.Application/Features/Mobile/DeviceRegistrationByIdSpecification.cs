using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Mobile;

public sealed class DeviceRegistrationByIdSpecification : ISpecification<DeviceRegistration>
{
    public DeviceRegistrationByIdSpecification(TenantId tenantId, DeviceRegistrationId id)
    {
        Criteria = d => d.TenantId == tenantId && d.Id == id;
    }

    public Expression<Func<DeviceRegistration, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<DeviceRegistration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<DeviceRegistration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
