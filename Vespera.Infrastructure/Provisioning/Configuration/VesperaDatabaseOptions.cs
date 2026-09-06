namespace Vespera.Infrastructure.Provisioning.Configuration;

public sealed class VesperaDatabaseOptions
{
    public const string SectionName = "Vespera:Database";

    public DatabaseMode Mode { get; set; } = DatabaseMode.Auto;

    public DatabaseEngine Engine { get; set; } = DatabaseEngine.Postgres;

    public LocalDatabaseOptions Local { get; set; } = new();

    public DockerDatabaseOptions Docker { get; set; } = new();

    public ExternalDatabaseOptions External { get; set; } = new();

    public FallbackDatabaseOptions Fallback { get; set; } = new();
}

public sealed class LocalDatabaseOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5432;

    public string Database { get; set; } = "vespera";

    public string Username { get; set; } = "postgres";

    public string Password { get; set; } = "postgres";
}

public sealed class DockerDatabaseOptions
{
    public string Image { get; set; } = "postgres";

    public string Tag { get; set; } = "16-alpine";

    public string ContainerName { get; set; } = "vespera-db-dev";

    public string VolumeName { get; set; } = "vespera-db-dev-data";

    public int Port { get; set; } = 55432;

    public DockerCredentials Credentials { get; set; } = new();

    public DockerPullPolicy PullPolicy { get; set; } = DockerPullPolicy.IfNotPresent;

    public bool ReuseExisting { get; set; } = true;

    public int StartupTimeoutSeconds { get; set; } = 60;

    /// <summary>When set, ProvisionAsync creates (if missing) and connects to this database
    /// instead of the server's default "postgres"/"master" catalog — the Docker-mode equivalent
    /// of FallbackDatabaseOptions.DatabaseFileName. Without this, every WebApplicationFactory
    /// instance shares one physical database on the reused dev container: DevelopmentSeeder
    /// reseeding the same tenant/employees from many concurrently-running test classes, and
    /// attendance/payroll rows from one test class colliding with another's. See
    /// VesperaWebApplicationFactory.</summary>
    public string? DatabaseName { get; set; }
}

public sealed class DockerCredentials
{
    public string Username { get; set; } = "postgres";

    public string Password { get; set; } = "postgres";
}

public sealed class ExternalDatabaseOptions
{
    public string? ConnectionStringName { get; set; }
}

public sealed class FallbackDatabaseOptions
{
    public bool UseSqlite { get; set; }

    /// <summary>Overrides the fallback database's file name (default "dev.db"), still resolved
    /// under &lt;ContentRootPath&gt;/.vespera/. Lets integration tests give each
    /// WebApplicationFactory instance an isolated database without touching provisioning
    /// internals — see VesperaWebApplicationFactory.</summary>
    public string? DatabaseFileName { get; set; }
}
