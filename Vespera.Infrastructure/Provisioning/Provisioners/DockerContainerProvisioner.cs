using System.Net;
using System.Net.Sockets;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;
using Polly.Retry;
using Vespera.Application.Abstractions.Provisioning;
using Vespera.Infrastructure.Provisioning.Configuration;

namespace Vespera.Infrastructure.Provisioning.Provisioners;

public sealed class DockerContainerProvisioner : IDatabaseProvisioner
{
    public const string ManagedLabel = "com.vespera.managed";

    private static readonly Action<ILogger, string, Exception?> LogReusingContainer = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(1, nameof(LogReusingContainer)), "Reusing existing container '{ContainerName}'");

    private static readonly Action<ILogger, int, int, Exception?> LogPortTaken = LoggerMessage.Define<int, int>(
        LogLevel.Information, new EventId(2, nameof(LogPortTaken)), "Configured Docker port {ConfiguredPort} is taken; using {HostPort} instead");

    private static readonly Action<ILogger, string, string, int, Exception?> LogContainerCreated =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Information, new EventId(3, nameof(LogContainerCreated)), "Created container '{ContainerName}' ({ContainerId}) on port {HostPort}");

    private static readonly Action<ILogger, string, string, Exception?> LogPullingImage = LoggerMessage.Define<string, string>(
        LogLevel.Information, new EventId(4, nameof(LogPullingImage)), "Pulling Docker image '{Image}:{Tag}'");

    private static readonly Action<ILogger, int, string?, Exception?> LogWaitingForReadiness = LoggerMessage.Define<int, string?>(
        LogLevel.Debug, new EventId(5, nameof(LogWaitingForReadiness)), "Waiting for database container readiness (attempt {Attempt}): {Reason}");

    private readonly IOptions<VesperaDatabaseOptions> _options;
    private readonly ILogger<DockerContainerProvisioner> _logger;

    public DockerContainerProvisioner(IOptions<VesperaDatabaseOptions> options, ILogger<DockerContainerProvisioner> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<string> ProvisionAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var docker = options.Docker;
        var containerPort = GetContainerPort(options.Engine);

        using var client = new DockerClientConfiguration().CreateClient();

        var containerId = await FindExistingContainerAsync(client, docker.ContainerName, cancellationToken);
        int hostPort;

        if (containerId is not null)
        {
            LogReusingContainer(_logger, docker.ContainerName, null);
            await EnsureRunningAsync(client, containerId, cancellationToken);
            hostPort = await GetBoundHostPortAsync(client, containerId, containerPort, cancellationToken) ?? docker.Port;
        }
        else
        {
            await EnsureImageAsync(client, docker, cancellationToken);

            hostPort = FindAvailablePort(docker.Port);
            if (hostPort != docker.Port)
            {
                LogPortTaken(_logger, docker.Port, hostPort, null);
            }

            containerId = await CreateContainerAsync(client, docker, options.Engine, containerPort, hostPort, cancellationToken);
            LogContainerCreated(_logger, docker.ContainerName, containerId[..12], hostPort, null);
            await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);
        }

        var adminConnectionString = BuildConnectionString(options.Engine, docker, hostPort, useDatabaseNameOverride: false);
        await WaitForReadinessAsync(options.Engine, adminConnectionString, docker.StartupTimeoutSeconds, cancellationToken);

        if (string.IsNullOrEmpty(docker.DatabaseName))
        {
            return adminConnectionString;
        }

        await EnsureDatabaseExistsAsync(options.Engine, adminConnectionString, docker.DatabaseName, cancellationToken);
        return BuildConnectionString(options.Engine, docker, hostPort, useDatabaseNameOverride: true);
    }

    private static async Task EnsureDatabaseExistsAsync(
        DatabaseEngine engine, string adminConnectionString, string databaseName, CancellationToken cancellationToken)
    {
        switch (engine)
        {
            case DatabaseEngine.Postgres:
            {
                await using var connection = new NpgsqlConnection(adminConnectionString);
                await connection.OpenAsync(cancellationToken);

                await using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
                checkCommand.Parameters.AddWithValue("name", databaseName);
                if (await checkCommand.ExecuteScalarAsync(cancellationToken) is not null)
                {
                    return;
                }

                // Database identifiers can't be parametrized; databaseName is generated by this
                // codebase itself (see VesperaWebApplicationFactory), never external input.
                await using var createCommand = connection.CreateCommand();
                createCommand.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await createCommand.ExecuteNonQueryAsync(cancellationToken);
                break;
            }

            case DatabaseEngine.SqlServer:
            {
                await using var connection = new SqlConnection(adminConnectionString);
                await connection.OpenAsync(cancellationToken);

                await using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT 1 FROM sys.databases WHERE name = @name";
                checkCommand.Parameters.AddWithValue("@name", databaseName);
                if (await checkCommand.ExecuteScalarAsync(cancellationToken) is not null)
                {
                    return;
                }

                await using var createCommand = connection.CreateCommand();
                createCommand.CommandText = $"CREATE DATABASE [{databaseName}]";
                await createCommand.ExecuteNonQueryAsync(cancellationToken);
                break;
            }

            default:
                throw new NotSupportedException($"Unsupported database engine '{engine}'.");
        }
    }

    private static async Task<string?> FindExistingContainerAsync(DockerClient client, string containerName, CancellationToken cancellationToken)
    {
        var containers = await client.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = new Dictionary<string, bool> { [containerName] = true },
            },
        }, cancellationToken);

        // Docker's name filter matches substrings; confirm an exact match. Names are reported with a leading '/'.
        return containers.FirstOrDefault(c => c.Names.Any(n => n.TrimStart('/') == containerName))?.ID;
    }

    private static async Task EnsureRunningAsync(DockerClient client, string containerId, CancellationToken cancellationToken)
    {
        var inspection = await client.Containers.InspectContainerAsync(containerId, cancellationToken);
        if (!inspection.State.Running)
        {
            await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);
        }
    }

    private static async Task<int?> GetBoundHostPortAsync(DockerClient client, string containerId, int containerPort, CancellationToken cancellationToken)
    {
        var inspection = await client.Containers.InspectContainerAsync(containerId, cancellationToken);
        var key = $"{containerPort}/tcp";

        if (inspection.HostConfig.PortBindings is { } bindings &&
            bindings.TryGetValue(key, out var portBindings) &&
            portBindings.Count > 0 &&
            int.TryParse(portBindings[0].HostPort, out var hostPort))
        {
            return hostPort;
        }

        return null;
    }

    private async Task EnsureImageAsync(DockerClient client, DockerDatabaseOptions docker, CancellationToken cancellationToken)
    {
        if (docker.PullPolicy == DockerPullPolicy.Never)
        {
            return;
        }

        if (docker.PullPolicy == DockerPullPolicy.IfNotPresent)
        {
            var images = await client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool> { [$"{docker.Image}:{docker.Tag}"] = true },
                },
            }, cancellationToken);

            if (images.Count > 0)
            {
                return;
            }
        }

        LogPullingImage(_logger, docker.Image, docker.Tag, null);
        await client.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = docker.Image, Tag = docker.Tag },
            null,
            new Progress<JSONMessage>(),
            cancellationToken);
    }

    private static async Task<string> CreateContainerAsync(
        DockerClient client, DockerDatabaseOptions docker, DatabaseEngine engine, int containerPort, int hostPort, CancellationToken cancellationToken)
    {
        var portKey = $"{containerPort}/tcp";

        var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = docker.ContainerName,
            Image = $"{docker.Image}:{docker.Tag}",
            Env = GetEnvironmentVariables(engine, docker.Credentials),
            Labels = new Dictionary<string, string> { [ManagedLabel] = "true" },
            ExposedPorts = new Dictionary<string, EmptyStruct> { [portKey] = default },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    [portKey] = [new PortBinding { HostIP = "127.0.0.1", HostPort = hostPort.ToString() }],
                },
                Binds = [$"{docker.VolumeName}:{GetDataVolumePath(engine)}"],
            },
        }, cancellationToken);

        return response.ID;
    }

    private async Task WaitForReadinessAsync(DatabaseEngine engine, string connectionString, int startupTimeoutSeconds, CancellationToken cancellationToken)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is not null),
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(250),
                MaxDelay = TimeSpan.FromSeconds(5),
                MaxRetryAttempts = int.MaxValue,
                UseJitter = true,
                OnRetry = args =>
                {
                    LogWaitingForReadiness(_logger, (int)args.AttemptNumber + 1, args.Outcome.Exception?.Message, null);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(startupTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await pipeline.ExecuteAsync(async ct => await DatabaseHandshake.PingAsync(engine, connectionString, ct), linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Database container did not become ready within {startupTimeoutSeconds}s.");
        }
    }

    private static int FindAvailablePort(int preferredPort)
    {
        if (IsPortFree(preferredPort))
        {
            return preferredPort;
        }

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static bool IsPortFree(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static int GetContainerPort(DatabaseEngine engine) => engine switch
    {
        DatabaseEngine.Postgres => 5432,
        DatabaseEngine.SqlServer => 1433,
        _ => throw new NotSupportedException($"Unsupported database engine '{engine}'."),
    };

    private static string GetDataVolumePath(DatabaseEngine engine) => engine switch
    {
        DatabaseEngine.Postgres => "/var/lib/postgresql/data",
        DatabaseEngine.SqlServer => "/var/opt/mssql",
        _ => throw new NotSupportedException($"Unsupported database engine '{engine}'."),
    };

    private static IList<string> GetEnvironmentVariables(DatabaseEngine engine, DockerCredentials credentials) => engine switch
    {
        DatabaseEngine.Postgres =>
        [
            $"POSTGRES_USER={credentials.Username}",
            $"POSTGRES_PASSWORD={credentials.Password}",
        ],
        DatabaseEngine.SqlServer =>
        [
            "ACCEPT_EULA=Y",
            $"MSSQL_SA_PASSWORD={credentials.Password}",
        ],
        _ => throw new NotSupportedException($"Unsupported database engine '{engine}'."),
    };

    private static string BuildConnectionString(DatabaseEngine engine, DockerDatabaseOptions docker, int hostPort, bool useDatabaseNameOverride)
    {
        var database = useDatabaseNameOverride ? docker.DatabaseName : null;
        return engine switch
        {
            DatabaseEngine.Postgres =>
                $"Host=127.0.0.1;Port={hostPort};Username={docker.Credentials.Username};Password={docker.Credentials.Password};Database={database ?? "postgres"}",
            DatabaseEngine.SqlServer =>
                $"Server=127.0.0.1,{hostPort};User Id=sa;Password={docker.Credentials.Password};Database={database ?? "master"};TrustServerCertificate=true",
            _ => throw new NotSupportedException($"Unsupported database engine '{engine}'."),
        };
    }
}
