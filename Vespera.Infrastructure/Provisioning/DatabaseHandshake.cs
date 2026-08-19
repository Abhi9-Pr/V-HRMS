using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using Vespera.Infrastructure.Provisioning.Configuration;

namespace Vespera.Infrastructure.Provisioning;

/// <summary>Opens a real driver connection for the given engine and runs SELECT 1. An open TCP
/// socket alone never satisfies a probe — this is the "real driver handshake" step.</summary>
internal static class DatabaseHandshake
{
    public static async Task PingAsync(DatabaseEngine engine, string connectionString, CancellationToken cancellationToken)
    {
        await using DbConnection connection = engine switch
        {
            DatabaseEngine.Postgres => new NpgsqlConnection(connectionString),
            DatabaseEngine.SqlServer => new SqlConnection(connectionString),
            _ => throw new NotSupportedException($"Unsupported database engine '{engine}'."),
        };

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
