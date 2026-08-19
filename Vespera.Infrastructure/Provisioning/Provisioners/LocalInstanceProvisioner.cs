using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;

namespace Vespera.Infrastructure.Provisioning.Provisioners;

public sealed class LocalInstanceProvisioner : IDatabaseProvisioner
{
    private static readonly Action<ILogger, string, Exception?> LogCreatingDatabase = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(1, nameof(LogCreatingDatabase)), "Local database '{Database}' does not exist; creating it");

    private readonly IOptions<VesperaDatabaseOptions> _options;
    private readonly ILogger<LocalInstanceProvisioner> _logger;

    public LocalInstanceProvisioner(IOptions<VesperaDatabaseOptions> options, ILogger<LocalInstanceProvisioner> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<string> ProvisionAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;

        return options.Engine switch
        {
            DatabaseEngine.Postgres => ProvisionPostgresAsync(options.Local, cancellationToken),
            DatabaseEngine.SqlServer => ProvisionSqlServerAsync(options.Local, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported database engine '{options.Engine}'."),
        };
    }

    private async Task<string> ProvisionPostgresAsync(LocalDatabaseOptions local, CancellationToken cancellationToken)
    {
        var maintenanceConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = local.Host,
            Port = local.Port,
            Username = local.Username,
            Password = local.Password,
            Database = "postgres",
        }.ConnectionString;

        await using (var connection = new NpgsqlConnection(maintenanceConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            await using var existsCommand = connection.CreateCommand();
            existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
            existsCommand.Parameters.AddWithValue("name", local.Database);
            var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;

            if (!exists)
            {
                LogCreatingDatabase(_logger, local.Database, null);
                await using var createCommand = connection.CreateCommand();
                createCommand.CommandText = $"CREATE DATABASE {QuotePostgresIdentifier(local.Database)}";
                await createCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = local.Host,
            Port = local.Port,
            Username = local.Username,
            Password = local.Password,
            Database = local.Database,
        }.ConnectionString;
    }

    private static async Task<string> ProvisionSqlServerAsync(LocalDatabaseOptions local, CancellationToken cancellationToken)
    {
        var maintenanceConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = $"{local.Host},{local.Port}",
            UserID = local.Username,
            Password = local.Password,
            InitialCatalog = "master",
            TrustServerCertificate = true,
        }.ConnectionString;

        await using (var connection = new SqlConnection(maintenanceConnectionString))
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID(@name) IS NULL EXEC('CREATE DATABASE {QuoteSqlIdentifier(local.Database)}')";
            command.Parameters.AddWithValue("@name", local.Database);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new SqlConnectionStringBuilder
        {
            DataSource = $"{local.Host},{local.Port}",
            UserID = local.Username,
            Password = local.Password,
            InitialCatalog = local.Database,
            TrustServerCertificate = true,
        }.ConnectionString;
    }

    private static string QuotePostgresIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    private static string QuoteSqlIdentifier(string identifier) => "[" + identifier.Replace("]", "]]") + "]";
}
