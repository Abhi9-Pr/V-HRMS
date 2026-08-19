using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Infrastructure.Persistence.Interceptors;

namespace Vespera.Infrastructure.UnitTests.Persistence;

public class TenantGuardInterceptorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SavingChanges_Should_Throw_When_Inserted_Entity_TenantId_Differs_From_Ambient_Tenant()
    {
        var ambientTenantId = TenantId.New();
        var otherTenantId = TenantId.New();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);
        tenantContext.TenantId.Returns(ambientTenantId);

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector(), new TenantGuardInterceptor(tenantContext));

        var department = Department.Create(otherTenantId, "Engineering", "ENG", null, Now, "seed").Value;
        dbContext.Add(department);

        var act = async () => await dbContext.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<TenantIsolationViolationException>();
    }

    [Fact]
    public async Task SavingChanges_Should_Allow_Insert_When_TenantId_Matches_Ambient_Tenant()
    {
        var ambientTenantId = TenantId.New();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);
        tenantContext.TenantId.Returns(ambientTenantId);

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector(), new TenantGuardInterceptor(tenantContext));

        var department = Department.Create(ambientTenantId, "Engineering", "ENG", null, Now, "seed").Value;
        dbContext.Add(department);

        var act = async () => await dbContext.SaveChangesAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
