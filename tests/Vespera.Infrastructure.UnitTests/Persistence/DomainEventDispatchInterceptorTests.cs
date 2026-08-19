using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Infrastructure.Persistence.Interceptors;
using Vespera.Infrastructure.Persistence.Outbox;

namespace Vespera.Infrastructure.UnitTests.Persistence;

public class DomainEventDispatchInterceptorTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_Should_Write_One_Outbox_Row_Per_Raised_Domain_Event_In_The_Same_Call()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);
        tenantContext.TenantId.Returns(TenantId);

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector(), new DomainEventDispatchInterceptor());

        var employee = Employee.Onboard(
            TenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
            EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 1), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            Now, "seed").Value;

        employee.DomainEvents.Should().ContainSingle();
        dbContext.Add(employee);

        var affected = await dbContext.SaveChangesAsync(CancellationToken.None);

        affected.Should().BeGreaterThan(1, "the entity insert and the outbox row must commit in the same SaveChanges call");
        employee.DomainEvents.Should().BeEmpty();

        var outboxRows = await dbContext.Set<OutboxMessageEntity>().ToListAsync(CancellationToken.None);
        outboxRows.Should().ContainSingle();
        outboxRows[0].Status.Should().Be(OutboxMessageStatus.Pending);
        outboxRows[0].Type.Should().Contain("EmployeeOnboarded");
    }
}
