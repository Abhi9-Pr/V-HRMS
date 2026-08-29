using System.Security.Cryptography;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

public sealed class GetDepartmentsETagQueryHandler : IRequestHandler<GetDepartmentsETagQuery, Result<string>>
{
    private readonly IReadRepository<Department> _departments;
    private readonly ITenantContext _tenantContext;

    public GetDepartmentsETagQueryHandler(IReadRepository<Department> departments, ITenantContext tenantContext)
    {
        _departments = departments;
        _tenantContext = tenantContext;
    }

    public async Task<Result<string>> Handle(GetDepartmentsETagQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var departments = await _departments.ListAsync(new DepartmentsByTenantSpecification(tenantId), cancellationToken);

        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(tenantId.Value.ToByteArray());
            foreach (var department in departments.OrderBy(d => d.Id.Value))
            {
                writer.Write(department.Id.Value.ToByteArray());
                writer.Write(department.RowVersion);
            }
        }

        return Result.Success(Convert.ToHexString(SHA256.HashData(buffer.ToArray())));
    }
}
