using Docker.DotNet;
using Docker.DotNet.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Vespera.Infrastructure.Provisioning.Configuration;
using Vespera.Infrastructure.Provisioning.Provisioners;

namespace Vespera.Infrastructure.UnitTests.Provisioning;

/// <summary>
/// Exercises DockerContainerProvisioner against a real Docker daemon. Skips (not fails) when no
/// daemon is reachable, so this test is safe to include in the normal `dotnet test` run.
/// </summary>
public class DockerContainerProvisioningIntegrationTests : IAsyncLifetime
{
    private const string TestContainerName = "vespera-db-test-integration";
    private const string TestVolumeName = "vespera-db-test-integration-data";

    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            using var client = new DockerClientConfiguration().CreateClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await client.System.PingAsync(cts.Token);
            _dockerAvailable = true;
        }
        catch
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (!_dockerAvailable)
        {
            return;
        }

        try
        {
            using var client = new DockerClientConfiguration().CreateClient();
            await client.Containers.RemoveContainerAsync(TestContainerName, new ContainerRemoveParameters { Force = true });
            await client.Volumes.RemoveAsync(TestVolumeName, force: true);
        }
        catch
        {
            // Best-effort cleanup; nothing to assert on here.
        }
    }

    [SkippableFact]
    public async Task ProvisionAsync_Should_Start_A_Real_Managed_Container_And_Connect()
    {
        Skip.IfNot(_dockerAvailable, "Docker daemon is not reachable in this environment");

        var options = Options.Create(new VesperaDatabaseOptions
        {
            Engine = DatabaseEngine.Postgres,
            Docker = new DockerDatabaseOptions
            {
                Image = "postgres",
                Tag = "16-alpine",
                ContainerName = TestContainerName,
                VolumeName = TestVolumeName,
                Port = 55199,
                Credentials = new DockerCredentials { Username = "postgres", Password = "postgres" },
                PullPolicy = DockerPullPolicy.IfNotPresent,
                ReuseExisting = true,
                StartupTimeoutSeconds = 90,
            },
        });

        var provisioner = new DockerContainerProvisioner(options, NullLogger<DockerContainerProvisioner>.Instance);

        var connectionString = await provisioner.ProvisionAsync(CancellationToken.None);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync();
            result.Should().Be(1);
        }

        using var client = new DockerClientConfiguration().CreateClient();
        var inspection = await client.Containers.InspectContainerAsync(TestContainerName);
        inspection.Config.Labels.Should().ContainKey(DockerContainerProvisioner.ManagedLabel).WhoseValue.Should().Be("true");
    }
}
