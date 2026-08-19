using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Infrastructure.UnitTests.Persistence;

public class ConcurrencyConflictTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_Should_Throw_ConcurrencyConflictException_Not_The_Raw_Provider_Exception_On_A_Stale_RowVersion()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);
        tenantContext.TenantId.Returns(TenantId);

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector());

        var department = Department.Create(TenantId, "Engineering", "ENG", null, Now, "seed").Value;
        dbContext.Add(department);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        department.Rename("Platform Engineering", Now, "seed");
        var entry = dbContext.Entry(department);
        entry.Property("RowVersion").OriginalValue = Guid.NewGuid().ToByteArray();

        var act = async () => await dbContext.SaveChangesAsync(CancellationToken.None);

        // A type assertion alone proves the raw provider exception (DbUpdateConcurrencyException,
        // unrelated to this type) never escapes SaveChangesAsync — see VesperaDbContext.
        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
