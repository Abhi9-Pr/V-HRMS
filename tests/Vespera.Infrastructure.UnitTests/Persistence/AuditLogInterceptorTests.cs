using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence.Auditing;
using Vespera.Infrastructure.Persistence.Interceptors;

namespace Vespera.Infrastructure.UnitTests.Persistence;

public class AuditLogInterceptorTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SavingChanges_Should_Write_Redacted_AuditLog_Row_When_Pii_Property_Changes()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        currentUser.IpAddress.Returns("127.0.0.1");
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);
        tenantContext.TenantId.Returns(TenantId);

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector(), new AuditLogInterceptor(currentUser));

        var employee = Employee.Onboard(
            TenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
            EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 1), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            Now, "seed").Value;
        dbContext.Add(employee);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        const string rawPan = "ABCDE1234F";
        employee.UpdateStatutoryDetails(PanNumber.Create(rawPan).Value, null, Now, "seed");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var auditRows = await dbContext.Set<AuditLogEntity>().ToListAsync(CancellationToken.None);
        var updateRow = auditRows.Should().ContainSingle(r => r.ActionType == AuditActionType.Modified).Subject;

        updateRow.NewValueJson.Should().NotBeNullOrEmpty();
        updateRow.NewValueJson!.Should().NotContain(rawPan);
        updateRow.IpAddress.Should().Be("127.0.0.1");
    }
}
