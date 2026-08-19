using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;

namespace Vespera.Infrastructure.Persistence.Design;

/// <summary>
/// Used only by `dotnet ef migrations add` (design time), never at runtime. The tenant filter and
/// PII-column conversions only affect query/write shape, not the migration's DDL, so trivial
/// no-op services are enough to build the model for scaffolding.
/// </summary>
public sealed class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<VesperaDbContext>
{
    public VesperaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VesperaDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=vespera_designtime");

        return new VesperaDbContext(optionsBuilder.Options, new DesignTimeTenantContext(), new DesignTimePiiProtector());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public TenantId TenantId => default;

        public bool HasTenant => false;
    }

    private sealed class DesignTimePiiProtector : IPiiProtector
    {
        public string Protect(string plainText) => plainText;

        public string Unprotect(string protectedText) => protectedText;
    }
}
