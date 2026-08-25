using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Infrastructure.Persistence.Auditing;
using Vespera.Infrastructure.Security;
using Vespera.Infrastructure.UnitTests.Persistence;

namespace Vespera.Infrastructure.UnitTests.Security;

public class PiiAccessAuditorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordAccessAsync_Should_Stage_A_PiiAccessAuditEntry_Row()
    {
        var tenantId = TenantId.New();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.HasTenant.Returns(true);
        tenantContext.TenantId.Returns(tenantId);

        using var factory = new SqliteVesperaDbContextFactory();
        using var dbContext = factory.Create(tenantContext, new FakePiiProtector());

        var subjectId = Guid.NewGuid();
        var auditor = new PiiAccessAuditor(dbContext);

        await auditor.RecordAccessAsync(tenantId, "Employee", subjectId, "Pan", "hr@vespera.test", Now, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var stored = dbContext.Set<PiiAccessAuditEntry>().Single();
        stored.TenantId.Should().Be(tenantId.Value);
        stored.SubjectType.Should().Be("Employee");
        stored.SubjectId.Should().Be(subjectId);
        stored.Field.Should().Be("Pan");
        stored.AccessedBy.Should().Be("hr@vespera.test");
        stored.OccurredAt.Should().Be(Now);
    }
}
