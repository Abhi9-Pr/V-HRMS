using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Infrastructure.Persistence.Interceptors;

namespace Vespera.Infrastructure.UnitTests.Persistence;

public class AuditableEntityInterceptorTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ModifiedAt = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SavingChanges_Should_Stamp_CreatedAt_CreatedBy_And_RowVersion_On_Insert()
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(CreatedAt);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);
        tenantContext.TenantId.Returns(TenantId);

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector(), new AuditableEntityInterceptor(dateTimeProvider, currentUser));

        var department = Department.Create(TenantId, "Engineering", "ENG", null, CreatedAt, "seed").Value;
        dbContext.Add(department);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        department.CreatedAt.Should().Be(CreatedAt);
        department.CreatedBy.Should().Be("system");
        department.RowVersion.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SavingChanges_Should_Stamp_ModifiedAt_And_Regenerate_RowVersion_On_Update()
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(CreatedAt);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);
        tenantContext.TenantId.Returns(TenantId);

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector(), new AuditableEntityInterceptor(dateTimeProvider, currentUser));

        var department = Department.Create(TenantId, "Engineering", "ENG", null, CreatedAt, "seed").Value;
        dbContext.Add(department);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var rowVersionAfterInsert = department.RowVersion;

        dateTimeProvider.UtcNow.Returns(ModifiedAt);
        department.Rename("Platform Engineering", ModifiedAt, "someone");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        department.ModifiedAt.Should().Be(ModifiedAt);
        department.ModifiedBy.Should().Be("system");
        department.RowVersion.Should().NotBeEquivalentTo(rowVersionAfterInsert);
    }
}
