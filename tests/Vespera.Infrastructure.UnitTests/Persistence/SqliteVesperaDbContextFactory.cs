using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Persistence;

namespace Vespera.Infrastructure.UnitTests.Persistence;

/// <summary>
/// Builds a <see cref="VesperaDbContext"/> against a real (in-memory, connection-kept-open)
/// SQLite database, schema created via <c>EnsureCreated</c> — no Docker/Postgres/SQL Server
/// needed, so these tests always run. Disposing the returned handle closes the connection and
/// drops the in-memory database.
/// </summary>
internal sealed class SqliteVesperaDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteVesperaDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public VesperaDbContext Create(
        ITenantContext tenantContext, IPiiProtector piiProtector, params ISaveChangesInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VesperaDbContext>()
            .UseSqlite(_connection)
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, NonCachingModelCacheKeyFactory>();

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        var context = new VesperaDbContext(optionsBuilder.Options, tenantContext, piiProtector);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
